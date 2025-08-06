using Mars.Core.Exceptions;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Controllers.Plugins;
using Mars.Test.Common.FixtureCustomizes;
using FluentAssertions;

namespace Mars.WebApiClient.Integration.Tests.Tests.Plugins;

public class GetPluginTests : BaseWebApiClientTests
{
    public GetPluginTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async void ListPlugin_Request_Unauthorized()
    {
        //Arrange
        var client = GetWebApiClient(true);

        //Act
        var action = () => client.Plugin.ListTable(new());

        //Assert
        await action.Should().ThrowAsync<UnauthorizedException>();
    }

    [IntegrationFact]
    public async Task ListPlugin_ValidRequest_ShouldSuccess()
    {
        //Arrange
        var client = GetWebApiClient();
        _fixture.AddTestPlugin(AppFixture.ServiceProvider);

        //Act
        var list = await client.Plugin.List(new());
        var list2 = await client.Plugin.ListTable(new());

        //Assert
        list.Items.Should().HaveCountGreaterThanOrEqualTo(1);
        list2.Items.Should().HaveCountGreaterThanOrEqualTo(1);
    }

}
