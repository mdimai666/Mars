using Mars.Integration.Tests.Attributes;
using FluentAssertions;
using Flurl.Http;
using PluginExample.Controllers;

namespace Mars.Plugin.Integration.Tests.Tests;

public class PluginControllerTests : BasePluginTests
{
    private const string ApiUrl = "api/MyPlugin/PluginEndpoint";

    public PluginControllerTests(PluginApplicationFixture appFixture) : base(appFixture)
    {
    }

    [IntegrationFact]
    public async Task PluginEndpoint_ControllerWorked_ShouldResponseOK()
    {
        //Arrange
        _ = nameof(MyPluginController.PluginEndpoint);
        var client = AppFixture.GetClient();

        //Act
        var result = await client.Request(ApiUrl).GetStringAsync();

        //Assert
        result.Should().Be("OK");
    }
}
