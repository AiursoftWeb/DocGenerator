using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Aiursoft.DocGenerator.Services;
using Aiursoft.DocGenerator.Tools;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;

namespace Aiursoft.DocGenerator.Middlewares;

public class APIDocGeneratorMiddleware
{
    private static Func<MethodInfo, Type, bool>? _isAPIAction;
    private static Func<MethodInfo, Type, bool>? _judgeAuthorized;
    private static List<object> _globalPossibleResponse = new ();
    private static DocFormat _format;
    private static string? _docAddress;
    private readonly RequestDelegate _next;

    public APIDocGeneratorMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public static void ApplySettings(APIDocGeneratorSettings settings)
    {
        _isAPIAction = settings.IsApiAction;
        _judgeAuthorized = settings.RequiresAuthorized;
        _globalPossibleResponse = settings.GlobalApisPossibleResponses;
        _format = settings.Format;
        _docAddress = settings.DocAddress.TrimStart('/').ToLower();
    }

    public async Task Invoke(HttpContext context)
    {
        if (_isAPIAction == null || _judgeAuthorized == null)
        {
            throw new ArgumentNullException();
        }

        if (context.Request.Path.ToString().Trim().Trim('/').ToLower() != _docAddress)
        {
            await _next.Invoke(context);
            return;
        }

        switch (_format)
        {
            case DocFormat.Json:
                context.Response.ContentType = "application/json";
                break;
            case DocFormat.Markdown:
                context.Response.ContentType = "text/markdown";
                break;
            default:
                throw new InvalidDataException($"Invalid format: '{_format}'!");
        }

        context.Response.StatusCode = 200;
        var actionsMatches = new List<API>();
        var possibleControllers = Assembly
            .GetEntryAssembly()
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
                if (!IsAction(method) || !_isAPIAction(method, controller))
                {
                    continue;
                }

                var args = GetArguments(method);
                var possibleResponses = GetPossibleResponses(method);
                var api = new API(
                    controllerName: controller.Name,
                    actionName: method.Name,
                    isPost: method.CustomAttributes.Any(t => t.AttributeType == typeof(HttpPostAttribute)),
                    routes: method.GetCustomAttributes(typeof(RouteAttribute), true)
                        .Select(t => t as RouteAttribute)
                        .Select(t => t?.Template)
                        .Select(t => $"{controllerRoute}/{t}")
                        .ToList(),
                    arguments: args,
                    authRequired: _judgeAuthorized(method, controller),
                    possibleResponses: possibleResponses);
                if (!api.Routes.Any())
                {
                    api.Routes.Add($"{api.ControllerName.TrimController()}/{api.ActionName}");
                }

                actionsMatches.Add(api);
            }
        }

        var generatedJsonDoc = JsonConvert.SerializeObject(actionsMatches);
        if (_format == DocFormat.Json)
        {
            await context.Response.WriteAsync(generatedJsonDoc);
        }
        else if (_format == DocFormat.Markdown)
        {
            var generator = new MarkDownDocGenerator();
            var groupedControllers = actionsMatches.GroupBy(t => t.ControllerName);
            var finalMarkDown = string.Empty;
            foreach (var controllerDoc in groupedControllers)
            {
                finalMarkDown +=
                    generator.GenerateMarkDownForController(controllerDoc,
                        $"{context.Request.Scheme}://{context.Request.Host}") + "\r\n--------\r\n";
            }

            await context.Response.WriteAsync(finalMarkDown);
        }
    }

    private string[] GetPossibleResponses(MethodInfo action)
    {
        var possibleList = action.GetCustomAttributes(typeof(ProducesAttribute))
            .Select(t => (t as ProducesAttribute)!.Type!)
            .Select(t => t.Make())
            .Select(JsonConvert.SerializeObject)
            .ToList();
        possibleList.AddRange(
            _globalPossibleResponse.Select(JsonConvert.SerializeObject));
        return possibleList.ToArray();
    }

    private List<Argument> GetArguments(MethodInfo method)
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
                        required: JudgeRequired(prop.PropertyType, prop.CustomAttributes),
                        type: ConvertTypeToArgumentType(prop.PropertyType)
                    )));
            }
            else
            {
                args.Add(new Argument(
                    name: GetArgumentName(param, param.Name!),
                    required: !param.HasDefaultValue && JudgeRequired(param.ParameterType, param.CustomAttributes),
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
            !method.IsAbstract &&
            !method.IsVirtual &&
            !method.IsStatic &&
            !method.IsConstructor &&
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

    private bool JudgeRequired(Type source, IEnumerable<CustomAttributeData> attributes)
    {
        if (attributes.Any(t => t.AttributeType == typeof(RequiredAttribute)))
        {
            return true;
        }

        return source == typeof(int) || source == typeof(DateTime) || source == typeof(bool);
    }
}

public class API
{
    public API(
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