using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Aiursoft.DocGenerator.Services;
using Aiursoft.DocGenerator.Tools;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Aiursoft.DocGenerator.Middlewares;

public class DocGeneratorMiddleware
{
    private readonly RequestDelegate _next;
    private readonly DocGeneratorSettings _config;
    private readonly ILogger<DocGeneratorMiddleware> _logger;

    public DocGeneratorMiddleware(
        RequestDelegate next,
        ILoggerFactory loggerFactory, 
        IOptions<DocGeneratorSettings> options)
    {
        _next = next;
        _config = options.Value;
        _logger = loggerFactory.CreateLogger<DocGeneratorMiddleware>();
    }

    public async Task Invoke(HttpContext context)
    {
        if (_config.IsApiAction == null || _config.RequiresAuthorized == null || _config.GlobalApisPossibleResponses == null)
        {
            throw new ArgumentNullException();
        }

        if (context.Request.Path.ToString().Trim().Trim('/').ToLower() != _config.DocAddress.Trim().Trim('/').ToLower())
        {
            await _next.Invoke(context);
            return;
        }

        _logger.LogTrace("Requesting doc generator...");
        context.Response.ContentType = _config.Format switch
        {
            DocFormat.Json => "application/json",
            DocFormat.Markdown => "text/markdown",
            _ => throw new InvalidDataException($"Invalid format: '{_config.Format}'!")
        };

        context.Response.StatusCode = 200;
        var actionsMatches = new List<Api>();
        var possibleControllers = _config.ApiProject
            ?.GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type))
            .ToList();
        foreach (var controller in possibleControllers ?? new List<Type>())
        {
            if (!IsController(controller))
            {
                continue;
            }

            var controllerRoute = controller.GetCustomAttributes(typeof(RouteAttribute), true)
                .Select(t => t as RouteAttribute)
                .Select(t => t?.Template)
                .FirstOrDefault();
            foreach (var method in controller.GetMethods(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public))
            {
                if (!IsAction(method) || !_config.IsApiAction(method, controller))
                {
                    continue;
                }

                var args = GetArguments(method);
                var possibleResponses = GetPossibleResponses(method, _config.GlobalApisPossibleResponses);
                var api = new Api(
                    controllerName: controller.Name,
                    actionName: method.Name,
                    isPost: method.CustomAttributes.Any(t => t.AttributeType == typeof(HttpPostAttribute)),
                    routes: method.GetCustomAttributes(typeof(RouteAttribute), true)
                        .Select(t => t as RouteAttribute)
                        .Select(t => t?.Template)
                        .Select(t => $"{controllerRoute}/{t}")
                        .ToList(),
                    arguments: args,
                    authRequired: _config.RequiresAuthorized(method, controller),
                    possibleResponses: possibleResponses);
                if (!api.Routes.Any())
                {
                    api.Routes.Add($"{api.ControllerName.TrimController()}/{api.ActionName}");
                }

                actionsMatches.Add(api);
            }
        }

        var generatedJsonDoc = JsonConvert.SerializeObject(actionsMatches);
        if (_config.Format == DocFormat.Json)
        {
            await context.Response.WriteAsync(generatedJsonDoc);
        }
        else if (_config.Format == DocFormat.Markdown)
        {
            var generator = new MarkDownDocGenerator();
            var groupedControllers = actionsMatches.GroupBy(t => t.ControllerName);
            var finalMarkDown = groupedControllers.Aggregate(
                string.Empty, 
                (current, controllerDoc) => 
                    current 
                    + generator.GenerateMarkDownForController(controllerDoc, $"{context.Request.Scheme}://{context.Request.Host}") 
                    + "\r\n--------\r\n");

            await context.Response.WriteAsync(finalMarkDown);
        }
    }

    private string[] GetPossibleResponses(MemberInfo action, IReadOnlyCollection<object> globalApisPossibleResponses)
    {
        var instanceMaker = new InstanceMaker();
        var possibleList = action.GetCustomAttributes(typeof(ProducesAttribute))
            .Select(t => (t as ProducesAttribute)!.Type!)
            .Select(t => instanceMaker.Make(t))
            .Select(JsonConvert.SerializeObject)
            .ToList();
        possibleList.AddRange(
            globalApisPossibleResponses.Select(JsonConvert.SerializeObject));
        return possibleList.ToArray();
    }

    private List<Argument> GetArguments(MethodBase method)
    {
        var args = new List<Argument>();
        foreach (var param in method.GetParameters())
        {
            if (param.ParameterType.IsClass && param.ParameterType != typeof(string))
            {
                args.AddRange(param
                    .ParameterType
                    .GetProperties()
                    .Select(prop => new Argument(
                        name: GetArgumentName(prop, prop.Name),
                        required: GetIsRequired(prop.PropertyType, prop.CustomAttributes),
                        type: ConvertTypeToArgumentType(prop.PropertyType)
                    )));
            }
            else
            {
                args.Add(new Argument(
                    name: GetArgumentName(param, param.Name!),
                    required: !param.HasDefaultValue && GetIsRequired(param.ParameterType, param.CustomAttributes),
                    type: ConvertTypeToArgumentType(param.ParameterType)
                ));
            }
        }

        return args;
    }

    private string GetArgumentName(ICustomAttributeProvider property, string defaultName)
    {
        var propName = defaultName;
        var fromQuery = property.GetCustomAttributes(typeof(IModelNameProvider), true).FirstOrDefault();
        if (fromQuery == null)
        {
            return propName;
        }

        var queriedName = (fromQuery as IModelNameProvider)?.Name;
        if (!string.IsNullOrWhiteSpace(queriedName))
        {
            propName = queriedName;
        }

        return propName;
    }

    private bool IsController(Type type)
    {
        return
            type.Name.EndsWith("Controller") &&
            type.Name != "Controller" &&
            type.IsSubclassOf(typeof(ControllerBase)) &&
            type.IsPublic;
    }

    private bool IsAction(MethodInfo method)
    {
        return
            method is { IsAbstract: false, IsVirtual: false, IsStatic: false, IsConstructor: false } &&
            !method.IsDefined(typeof(NonActionAttribute)) &&
            !method.IsDefined(typeof(ObsoleteAttribute));
    }

    private ArgumentType ConvertTypeToArgumentType(Type t)
    {
        return
            t == typeof(int) ? ArgumentType.Number :
            t == typeof(int?) ? ArgumentType.Number :
            t == typeof(long) ? ArgumentType.Number :
            t == typeof(long?) ? ArgumentType.Number :
            t == typeof(string) ? ArgumentType.Text :
            t == typeof(DateTime) ? ArgumentType.Datetime :
            t == typeof(DateTime?) ? ArgumentType.Datetime :
            t == typeof(bool) ? ArgumentType.Boolean :
            t == typeof(bool?) ? ArgumentType.Boolean :
            t == typeof(string[]) ? ArgumentType.Collection :
            t == typeof(List<string>) ? ArgumentType.Collection :
            ArgumentType.Unknown;
    }

    private bool GetIsRequired(Type source, IEnumerable<CustomAttributeData> attributes)
    {
        if (attributes.Any(t => t.AttributeType == typeof(RequiredAttribute)))
        {
            return true;
        }

        return source == typeof(int) || source == typeof(DateTime) || source == typeof(bool);
    }
}

public class Api
{
    public Api(
        string controllerName, 
        string actionName, 
        bool authRequired, 
        bool isPost, 
        List<Argument> arguments, 
        string[] possibleResponses, 
        List<string> routes)
    {
        ControllerName = controllerName;
        ActionName = actionName;
        AuthRequired = authRequired;
        IsPost = isPost;
        Arguments = arguments;
        PossibleResponses = possibleResponses;
        Routes = routes;
    }

    public string ControllerName { get; set; }
    public string ActionName { get; set; }
    public bool AuthRequired { get; set; }
    public bool IsPost { get; set; }
    public List<Argument> Arguments { get; set; }
    public string[] PossibleResponses { get; set; }
    public List<string> Routes { get; set; }
}

public class Argument
{
    public Argument(
        string name,
        bool required,
        ArgumentType type)
    {
        Name = name;
        Required = required;
        Type = type;
    }

    public string Name { get; set; }
    public bool Required { get; set; }
    public ArgumentType Type { get; set; }
}

public enum ArgumentType
{
    Text = 0,
    Number = 1,
    Boolean = 2,
    Datetime = 3,
    Collection = 4,
    Unknown = 5
}