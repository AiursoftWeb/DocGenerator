using Aiursoft.CSTools.Tools;
using Aiursoft.WebTools;
using DemoDocApp;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aiursoft.DocGenerator.Tests;

[TestClass]
public class IntegrationTests
{
    private readonly int _port;
    private readonly string _endpointUrl;
    private IHost? _server;

    public IntegrationTests()
    {
        _port = Network.GetAvailablePort();
        _endpointUrl = $"http://localhost:{_port}";
    }

    [TestInitialize]
    public async Task TestInitialize()
    {
        _server = Extends.App<Startup>(Array.Empty<string>(), port: _port);
        await _server.StartAsync();
    }

    [TestCleanup]
    public async Task CleanServer()
    {
        if (_server != null)
        {
            await _server.StopAsync();
            _server.Dispose();
        }
    }

    [TestMethod]
    public async Task TestJsonDoc()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, _endpointUrl + "/my-doc-json");
        var client = new HttpClient();
        var result = await client.SendAsync(request);
        await result.Content.ReadAsStringAsync();
        Assert.AreEqual(200, (int)result.StatusCode);
    }
    
    [TestMethod]
    public async Task TestMarkdownDoc()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, _endpointUrl + "/my-doc-markdown");
        var client = new HttpClient();
        var result = await client.SendAsync(request);
        await result.Content.ReadAsStringAsync();
        Assert.AreEqual(200, (int)result.StatusCode);
    }
}