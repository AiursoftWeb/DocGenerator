using System.Diagnostics.CodeAnalysis;
using Aiursoft.AiurProtocol;
using Aiursoft.AiurProtocol.Server;
using Aiursoft.DocGenerator.Attributes;
using DemoDocApp.Sdk.Models.ApiAddressModels;
using DemoDocApp.Sdk.Models.ApiViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DemoDocApp.Controllers;

[ApiExceptionHandler(
    PassthroughRemoteErrors = true,
    PassthroughAiurServerException = true)]
[ApiModelStateChecker]
[GenerateDoc]
[ExcludeFromCodeCoverage]
public class HomeController : ControllerBase
{
    public IActionResult Index()
    {
        return this.Protocol(Code.ResultShown, "Welcome to this API project!");
    }

    [Route("home/no-action")]
    public IActionResult NoAction()
    {
        return this.Protocol(Code.NoActionTaken, "No action taken!");
    }

    [Authorize]
    public IActionResult AuthorizedApi()
    {
        return BadRequest(new { message = "This is not a valid Protocol response." });
    }

    [Produces(typeof(AiurValue<int>))]
    public IActionResult GetANumber()
    {
        return this.Protocol(Code.ResultShown, "Got your value!", value: 123);
    }

    [Produces(typeof(AiurCollection<int>))]
    public IActionResult QuerySomething([FromQuery] string question)
    {
        var items = Fibonacci()
            .Take(1024 * 1024)
            .Where(i => i.ToString().EndsWith(question))
            .Take(10)
            .ToList();
        return this.Protocol(Code.ResultShown, "Got your value!", items);
    }

    [Produces(typeof(AiurPagedCollection<int>))]
    public async Task<IActionResult> QuerySomethingPaged([FromQuery] QueryNumberAddressModel model)
    {
        var database = Fibonacci()
            .Take(30)
            .AsQueryable();
        var items = database
            .Where(i => i.ToString().EndsWith(model.Question ?? string.Empty))
            .AsQueryable()
            .OrderBy(i => i);
        return await this.Protocol(Code.ResultShown, "Got your value!", items, model);
    }

    [Produces(typeof(AiurCollection<int>))]
    public IActionResult GetFibonacciFirst10()
    {
        var items = Fibonacci().Take(10).ToList();
        return this.Protocol(Code.ResultShown, "Got your value!", items);
    }

    [HttpPost]
    [Produces(typeof(RegisterViewModel))]
    public IActionResult RegisterForm([FromForm] RegisterAddressModel model)
    {
        return this.Protocol(new RegisterViewModel
        {
            Code = Code.JobDone,
            Message = "Registered.",
            UserId = "your-id-" + model.Name
        });
    }

    [HttpPost]
    [Produces(typeof(RegisterViewModel))]
    public IActionResult RegisterJson([FromBody] RegisterAddressModel model)
    {
        return this.Protocol(new RegisterViewModel
        {
            Code = Code.JobDone,
            Message = "Registered.",
            UserId = "your-id-" + model.Name
        });
    }

    public IActionResult CrashKnown()
    {
        throw new AiurServerException(Code.Conflict, "Known error");
    }

    [ExcludeFromCodeCoverage]
    public IActionResult CrashUnknown()
    {
        var one = 1;
        // ReSharper disable once IntDivisionByZero
        _ = 3 / (1 - one);
        return Ok();
    }

    private IEnumerable<int> Fibonacci()
    {
        int current = 1, next = 1;

        while (true)
        {
            yield return current;
            next = current + (current = next);
        }
        // ReSharper disable once IteratorNeverReturns
    }
}