using FluentAssertions;
using Flurl.Http;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Middlewares;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Nodes;

public class HttpInNodeTests : ApplicationTests
{
    const string _apiUrl = "";
    private readonly INodeService _nodeService;

    public HttpInNodeTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _nodeService = AppFixture.ServiceProvider.GetRequiredService<INodeService>();
    }

    void SetupNodes(string url)
    {
        var nodes = NodesWorkflowBuilder.Create()
                                        .AddNext(new HttpInNode { Method = "GET", UrlPattern = url })
                                        .AddNext(new TemplateNode { Template = "OK" })
                                        .AddNext(new HttpResponseNode())
                                        .BuildWithFlowNode();
        _nodeService.Deploy(nodes);
    }

    [IntegrationFact]
    public async Task TryMatch_InvalidUrlRequest_ShouldNotOK()
    {
        //Arrange
        _ = nameof(HttpInNode);
        _ = nameof(MarsNodesMiddleware);
        _ = nameof(HttpCatchRegister.TryMatch);
        var client = AppFixture.GetClient();
        SetupNodes("/url1");

        //Act
        var result = await client.Request(_apiUrl, "invalid").AllowAnyHttpStatus().GetStringAsync();

        //Assert
        result.Should().NotBe("OK");
    }

    [IntegrationFact]
    public async Task TryMatch_StaticUrlRequest_ShouldOK()
    {
        //Arrange
        _ = nameof(HttpInNode);
        _ = nameof(MarsNodesMiddleware);
        _ = nameof(HttpCatchRegister.TryMatch);
        var client = AppFixture.GetClient();
        SetupNodes("/url1");

        //Act
        var result = await client.Request(_apiUrl, "url1").GetStringAsync();

        //Assert
        result.Should().Be("OK");
    }

    [IntegrationFact]
    public async Task TryMatch_PatternUrlRequest_ShouldOK()
    {
        //Arrange
        _ = nameof(HttpInNode);
        _ = nameof(MarsNodesMiddleware);
        _ = nameof(HttpCatchRegister.TryMatch);
        var client = AppFixture.GetClient();
        SetupNodes("/{pattern}");

        //Act
        var result = await client.Request(_apiUrl, "url1").GetStringAsync();

        //Assert
        result.Should().Be("OK");
    }

    [IntegrationFact]
    public async Task TryMatch_StaticUrlShouldBePrioritizedOverPatternUrl_ShouldMatchStatic()
    {
        //Arrange
        _ = nameof(HttpInNode);
        _ = nameof(MarsNodesMiddleware);
        _ = nameof(HttpCatchRegister.TryMatch);
        var client = AppFixture.GetClient();
        var nodes = NodesWorkflowBuilder.Create()
                                        .AddNext(new HttpInNode { Method = "GET", UrlPattern = "/{pattern}" })
                                        .AddNext(new TemplateNode { Template = "pattern" })
                                        .AddNext(new HttpResponseNode())
                                        .Add(new HttpInNode { Method = "GET", UrlPattern = "/static" })
                                        .AddNext(new TemplateNode { Template = "static" })
                                        .AddNext(new HttpResponseNode())
                                        .BuildWithFlowNode();
        _nodeService.Deploy(nodes);

        //Act
        var result = await client.Request(_apiUrl, "static").GetStringAsync();

        //Assert
        result.Should().Be("static");
    }

    [IntegrationFact]
    public async Task TryMatch_TypedPatternUrlRequest_ShouldOK()
    {
        //Arrange
        _ = nameof(HttpInNode);
        _ = nameof(MarsNodesMiddleware);
        _ = nameof(HttpCatchRegister.TryMatch);
        var client = AppFixture.GetClient();
        SetupNodes("/{number:int}");

        //Act
        var result = await client.Request(_apiUrl, "123").GetStringAsync();

        //Assert
        result.Should().Be("OK");
    }

    [IntegrationFact]
    public async Task TryMatch_TypedPatternUrlInvalidTypeRequest_ShouldNotOK()
    {
        //Arrange
        _ = nameof(HttpInNode);
        _ = nameof(MarsNodesMiddleware);
        _ = nameof(HttpCatchRegister.TryMatch);
        var client = AppFixture.GetClient();
        SetupNodes("/{number:int}");

        //Act
        var result = await client.Request(_apiUrl, "xstring").AllowAnyHttpStatus().GetStringAsync();

        //Assert
        result.Should().NotBe("OK");
    }
}
