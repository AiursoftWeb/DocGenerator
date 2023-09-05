using System.Diagnostics.CodeAnalysis;
using Aiursoft.AiurProtocol.Server;
using Aiursoft.DocGenerator.Services;
using Aiursoft.WebTools;
using Aiursoft.AiurProtocol;
using Aiursoft.WebTools.Models;
using Microsoft.AspNetCore.HttpOverrides;

namespace DemoApiApp;

public class Program
{
    [ExcludeFromCodeCoverage]
    public static async Task Main(string[] args)
    {
        var app = Extends.App<Startup>(args);
        await app.RunAsync();
    }
}

public class Startup : IWebStartup
{
    public void ConfigureServices(IConfiguration configuration, IWebHostEnvironment environment, IServiceCollection services)
    {
        services
            .AddControllers()
            .AddApplicationPart(typeof(Startup).Assembly)
            .AddAiurProtocol();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment environment)
    {
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAiursoftDocGenerator(options: option =>
        {
            option.DocAddress = "/my-doc-json";
            option.Format = DocFormat.Json;
            option.ApiProject = typeof(Startup).Assembly;
            option.GlobalApisPossibleResponses.Add(new AiurResponse
            {
                Code = Code.WrongKey,
                Message = "Some error."
            });
            option.GlobalApisPossibleResponses.Add(new AiurCollection<string>(new List<string> { "Some item is invalid!" })
            {
                Code = Code.InvalidInput,
                Message = "Your input contains several errors!"
            });
        });
        
        app.UseAiursoftDocGenerator(options: option =>
        {
            option.DocAddress = "/my-doc-markdown";
            option.Format = DocFormat.Markdown;
            option.ApiProject = typeof(Startup).Assembly;
            option.GlobalApisPossibleResponses.Add(new AiurResponse
            {
                Code = Code.WrongKey,
                Message = "Some error."
            });
            option.GlobalApisPossibleResponses.Add(new AiurCollection<string>(new List<string> { "Some item is invalid!" })
            {
                Code = Code.InvalidInput,
                Message = "Your input contains several errors!"
            });
        });
        app.UseEndpoints(endpoints => endpoints.MapDefaultControllerRoute());

    }
}