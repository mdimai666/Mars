using FluentAssertions;
using Flurl.Http;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Middlewares;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Nodes;

public class HttpResponseNodeTests : ApplicationTests
{
    const string _apiUrl = "";
    private readonly INodeService _nodeService;

    public HttpResponseNodeTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _nodeService = AppFixture.ServiceProvider.GetRequiredService<INodeService>();
    }

    [IntegrationFact]
    public async Task Execute_StatusCodeSetup_ShouldReturnCode()
    {
        //Arrange
        _ = nameof(HttpResponseNodeImpl.Execute);
        _ = nameof(MarsNodesMiddleware);
        _ = nameof(HttpCatchRegister.TryMatch);
        var client = AppFixture.GetClient();
        var nodes = NodesWorkflowBuilder.Create()
                                        .AddNext(new HttpInNode { Method = "GET", UrlPattern = "/url1" })
                                        .AddNext(new TemplateNode { Template = "OK" })
                                        .AddNext(new HttpResponseNode() { ResponseStatusCode = 201 })
                                        .BuildWithFlowNode();
        _nodeService.Deploy(nodes);

        //Act
        var result = await client.Request(_apiUrl, "url1").GetAsync();

        //Assert
        result.StatusCode.Should().Be(201);
    }
}
