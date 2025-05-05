using FluentAssertions;
using Flurl.Http;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Converters;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Host.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Controllers.Nodes;

public class GetNodesTests : ApplicationTests
{
    const string _apiUrl = "/api/Node";

    public GetNodesTests(ApplicationFixture appFixture) : base(appFixture)
    {
        //_fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task LoadNodes_ResponseJsonDeserializer_ShouldSuccess()
    {
        //Arrange
        _ = nameof(NodeController.Load);
        _ = nameof(INodeService.Load);
        _ = nameof(NodeJsonConverter);

        var client = AppFixture.GetClient();
        var nodeService = AppFixture.ServiceProvider.GetRequiredService<INodeService>();
        nodeService.Deploy([new FlowNode() { Id = "flow1" }, new InjectNode() { Container = "flow1" }]);

        //Act
        var result = await client.Request(_apiUrl, "Load").GetAsync().CatchUserActionError().ReceiveJson<List<Node>>();

        //Assert
        result.Count.Should().BeGreaterThan(0);
    }
}
