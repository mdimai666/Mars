using FluentAssertions;
using Flurl.Http;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Plugin.Contracts.Plugins;
using Mars.Plugin.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Controllers.Plugins;

public class PluginLifecycleTests : ApplicationTests
{
    const string _apiUrl = "/api/Plugin";
    private readonly PluginManager _pluginManager;

    public PluginLifecycleTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new Mars.Test.Common.FixtureCustomizes.FixtureCustomize());
        _pluginManager = appFixture.ServiceProvider.GetRequiredService<PluginManager>();
    }

    [IntegrationFact]
    public async Task SetEnabled_OnInstalledPlugin_Succeeds()
    {
        //Arrange
        var client = AppFixture.GetClient();
        _fixture.AddTestPlugin(AppFixture.ServiceProvider);
        var packageId = _pluginManager.Plugins.First().Info.PackageId;
        try
        {
            //Act
            var response = await client.Request(_apiUrl, "SetEnabled")
                .AllowAnyHttpStatus()
                .PostJsonAsync(new SetPluginEnabledRequest { PackageId = packageId, Enabled = false });

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status200OK);
        }
        finally
        {
            _pluginManager.RemovePlugin(packageId);
        }
    }

    [IntegrationFact]
    public async Task Uninstall_OnInstalledPlugin_Succeeds()
    {
        //Arrange
        var client = AppFixture.GetClient();
        _fixture.AddTestPlugin(AppFixture.ServiceProvider);
        var packageId = _pluginManager.Plugins.First().Info.PackageId;
        try
        {
            //Act
            var response = await client.Request(_apiUrl, "Uninstall")
                .AllowAnyHttpStatus()
                .PostJsonAsync(new UninstallPluginRequest { PackageId = packageId });

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status200OK);
        }
        finally
        {
            _pluginManager.RemovePlugin(packageId);
        }
    }

    [IntegrationFact]
    public async Task SetEnabled_OnUnknownPlugin_ReturnsUserActionError()
    {
        //Arrange
        var client = AppFixture.GetClient();

        //Act
        var response = await client.Request(_apiUrl, "SetEnabled")
            .AllowAnyHttpStatus()
            .PostJsonAsync(new SetPluginEnabledRequest { PackageId = "No.Such.Plugin", Enabled = true });

        //Assert
        response.StatusCode.Should().Be(466); // UserActionException
    }
}
