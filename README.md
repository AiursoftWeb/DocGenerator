# Aiursoft DocGenerator

<a name="aspnet-core-api-document-generator"></a>

[![MIT licensed](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/AiursoftWeb/docgenerator/blob/master/LICENSE)
[![Pipeline stat](https://badge-proxy.aiursoft.com/aiursoft/DocGenerator/badges/master/pipeline.svg)](https://gitlab.aiursoft.com/aiursoft/DocGenerator/-/pipelines)
[![Test Coverage](https://badge-proxy.aiursoft.com/aiursoft/DocGenerator/badges/master/coverage.svg)](https://gitlab.aiursoft.com/aiursoft/DocGenerator/-/pipelines)
[![S360 compliance](https://s360.aiursoft.com/badge.svg?project=aiursoft%2Fdocgenerator)](https://s360.aiursoft.com/SupplyChain?tab=projects&project=aiursoft%2Fdocgenerator&scope=baseline&ecosystem=all)
[![NuGet version (Aiursoft.DocGenerator)](https://img.shields.io/nuget/v/Aiursoft.DocGenerator.svg)](https://www.nuget.org/packages/Aiursoft.DocGenerator/)
[![Man hours](https://manhours.aiursoft.com/r/github.com/aiursoftweb/docgenerator.svg)](https://manhours.aiursoft.com/r/github.com/aiursoftweb/docgenerator.html)

A basic API document generator for ASP.NET Core applications. Open source, offline and free.

## Overview

### What is DocGenerator?

<a name="features"></a>

* Generate Markdown
* Generate Json

[Open the example](https://wiki.aiursoft.com/Kahla/Devices.md)

## Installation

Add the package to your .NET project:

<a name="how-to-install"></a>

First, install `Aiursoft.DocGenerator` to your ASP.NET Core project from nuget.org:

```bash
dotnet add package Aiursoft.DocGenerator
```

Requires .NET 10 or later.

## Usage

Use this complete ASP.NET Core `Program.cs` after installing the package, then browse `/doc`:

```csharp
using Aiursoft.DocGenerator.Attributes;
using Aiursoft.DocGenerator.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
var app = builder.Build();
app.UseAiursoftDocGenerator();
app.MapControllers();
app.Run();

[GenerateDoc]
public class ExampleController : ControllerBase
{
    [HttpGet("/hello")]
    public IActionResult Hello() => Ok("Hello");
}
```

### Middleware integration

Simply add this line in your `Startup.cs`:

```csharp
using Aiursoft.DocGenerator.Services;

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // ...
    // your middlewares
    // ...

    app.UseAiursoftDocGenerator(); // <- Add this.
}
```

Start your application and browse:

```
/doc
```

It just returns your document in JSON format.

```json
[{
	"ControllerName": "HomeController",
	"ActionName": "Index",
	"AuthRequired": false,
	"IsPost": false,
	"Arguments": [],
	"PossibleResponses": ["{\"code\":0,\"message\":\"success.\"}"]
}]
```

That's all! Happy coding!

Continue? Try runing the example project! Or continue reading.

## Configuration

<a name="customization-and-api"></a>

**Change document output address**

<a name="change-document-output-address"></a>

```csharp
app.UseAiursoftDocGenerator(options =>
{
    // Default value is '/doc'. You can change it to other path.
    options.DocAddress = "/my-doc";
});
```

**Change document output format**

<a name="change-document-output-format"></a>

```csharp
app.UseAiursoftDocGenerator(options =>
{
    // Default format is JSON. You can change it to markdown.
    options.Format = DocFormat.Markdown;
});
```

**Set global possible responses**

<a name="set-global-possible-responses"></a>

```csharp
app.UseAiursoftDocGenerator(options =>
{
    // Default global possible response is an empty list.
    options.GlobalApisPossibleResponses.Add(new { code = 0, message = "success." });
});
```

**Set possible response for one API**

<a name="set-possible-response-for-one-api"></a>

When you can ensure the possible response for one API, add this line to your action:

```csharp
[Produces(typeof(ResponseModel))] // <- add this in your controller.
public IActionResult HasOutput()
{
    var model = new ResponseModel(); // <- your own class and logic
    return Json(model);
}
```

**Document generation filter**

<a name="document-generation-filter"></a>

By default, only controllers and actions with `[GenerateDoc]` attributes will be generated.

To mark a controller or action which generates document, add attribute `[GenerateDoc]` like this:

```csharp
using Aiursoft.DocGenerator.Attributes;

[GenerateDoc] // Add this, the entire controller will generate document.
public class HomeController : Controller
{
    [GenerateDoc] // Add this, the action will generate document.
    public IActionResult MyAPI()
    {
        return Json(null);
    }
}
```

You can change that logic to your own filter:

```csharp
app.UseAiursoftDocGenerator(options =>
{
    options.IsApiAction = (action, controller) =>
    {
        // Your own logic. Return bool.
        return action.CustomAttributes.Any(t => t.AttributeType == typeof(GenerateDoc));
    };
});
```

**Authorized action detector**

<a name="authorized-action-detector"></a>

If your API is authorized required, we can detect that in the document. And you can customzie the logic:

```csharp
using Microsoft.AspNetCore.Authorization;

app.UseAiursoftDocGenerator(options =>
{
    options.RequiresAuthorized = (action, controller) =>
    {
        // Your own logic here. Return bool.
        return
            action.CustomAttributes.Any(t => t.AttributeType == typeof(AuthorizeAttribute)) ||
            controller.CustomAttributes.Any(t => t.AttributeType == typeof(AuthorizeAttribute));
    };
});
```

**Defaults**

`DocGeneratorSettings` uses the `doc` address (served as `/doc`), JSON format and an empty `GlobalApisPossibleResponses` list. By default it selects actions or controllers marked with `GenerateDoc`, checks `AuthorizeAttribute` for authorization metadata, and uses the entry assembly for `ApiProject`. Set these delegates and values in the middleware options; no separate configuration file is required.

## Development

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

From the repository root, restore, build and test:

```bash
dotnet restore Aiursoft.DocGenerator.sln
dotnet build Aiursoft.DocGenerator.sln --no-restore
dotnet test Aiursoft.DocGenerator.sln --no-restore
```

Run `./lint.sh` for the existing static checks.

## How to contribute

<a name="how-to-contribute"></a>

There are many ways to contribute to the project: logging bugs, submitting pull requests, reporting issues, and creating suggestions.

Even if you have push rights on the repository, you should create a personal fork and create feature branches there when you need them. This keeps the main repository clean and your workflow cruft out of sight.

We're also interested in your feedback on the future of this project. You can submit a suggestion or feature request through the issue tracker. To make this process more effective, we're asking that these include more information to help define them more clearly.

## License

Licensed under the [MIT License](https://github.com/AiursoftWeb/docgenerator/blob/master/LICENSE).
