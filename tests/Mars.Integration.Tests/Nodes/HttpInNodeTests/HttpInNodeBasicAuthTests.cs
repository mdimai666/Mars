using FluentAssertions;
using Flurl.Http;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Nodes.Core.Implements.Nodes.Network;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Nodes.Network;
using Mars.Nodes.Core.Utils;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Nodes.HttpInNodeTests;

public class HttpInNodeBasicAuthTests : ApplicationTests
{
    const string _endpointUrl = "/api2/http1";
    private readonly INodeService _nodeService;

    public HttpInNodeBasicAuthTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _nodeService = AppFixture.ServiceProvider.GetRequiredService<INodeService>();
    }

    void SetupNodes(HttpInNode endpointNode)
    {
        var nodes = NodesWorkflowBuilder.Create()
                                        .AddNext(endpointNode)
                                        .AddNext(new HttpResponseNode())
                                        .BuildWithFlowNode();
        _nodeService.Deploy(nodes);
    }

    [IntegrationFact]
    public async Task ValidateRequestRequirements_IsRequireAuthorize_ShouldStatus401()
    {
        //Arrange
        _ = nameof(HttpInNodeImpl.Execute);
        var client = AppFixture.GetClient(true);
        SetupNodes(new HttpInNode
        {
            Method = "POST",
            UrlPattern = _endpointUrl,
            IsRequireAuthorize = true
        });

        //Act
        var result = await client.Request(_endpointUrl).AllowAnyHttpStatus().PostJsonAsync(new { });

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task ValidateRequestRequirements_AllowedRoles_ShouldStatus403()
    {
        //Arrange
        _ = nameof(HttpInNodeImpl.Execute);
        var client = AppFixture.GetClient();
        SetupNodes(new HttpInNode
        {
            Method = "POST",
            UrlPattern = _endpointUrl,
            IsRequireAuthorize = true,
            AllowedRoles = ["Superuser"]
        });

        //Act
        var result = await client.Request(_endpointUrl).AllowAnyHttpStatus().PostJsonAsync(new { });

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }
}
