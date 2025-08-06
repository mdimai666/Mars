using System.Text;
using FluentAssertions;
using Mars.Core.Exceptions;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Controllers.Plugins;
using Mars.Plugin;
using Mars.Test.Common.FixtureCustomizes;

namespace Mars.WebApiClient.Integration.Tests.Tests.Plugins;

public class UploadPluginTests : BaseWebApiClientTests
{
    public UploadPluginTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async void UploadPlugin_Request_Unauthorized()
    {
        //Arrange
        var client = GetWebApiClient(true);

        //Act
        var action = () => client.Plugin.UploadPlugin([]);

        //Assert
        await action.Should().ThrowAsync<UnauthorizedException>();
    }

    [IntegrationFact]
    public async Task UploadPlugin_ValidRequest_ShouldSuccess()
    {
        //Arrange
        var client = GetWebApiClient();
        _fixture.AddTestPlugin(AppFixture.ServiceProvider);

        var pluginName = "Project.Plugin1";
        byte[] assemblyBin = [1, 2, 3];
        using var zip = ZipHelper.ZipFiles(new()
        {
            [pluginName + ".dll"] = assemblyBin,
            [pluginName + ".nuspec"] = Encoding.UTF8.GetBytes("<xml>123</xml>"),
            [pluginName + ".runtimeconfig.json"] = Encoding.UTF8.GetBytes("{}"),
        });

        //Act
        var result = await client.Plugin.UploadPlugin(
            (zip, $"{pluginName}.zip")
        );

        //Assert
        result.Items.Count.Should().Be(1);
        result.Items.First().Success.Should().BeTrue();
    }

}
