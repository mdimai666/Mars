using FluentAssertions;
using Flurl.Http;
using Mars.Contracts.Common;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Plugin.Contracts.Plugins;
using Mars.Plugin.Services;
using Mars.Server.Abstractions.Services;
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

    [IntegrationFact]
    public async Task Uninstall_OnInstalledPluginWithFolder_MarksPendingDelete_KeepsFolder()
    {
        //Arrange
        var client = AppFixture.GetClient();
        _fixture.AddTestPlugin(AppFixture.ServiceProvider);
        var packageId = _pluginManager.Plugins.First().Info.PackageId;
        var fileStorage = AppFixture.ServiceProvider.GetRequiredKeyedService<IFileStorage>("data");
        var dir = Path.Combine(PluginManager.PluginsDefaultPath, packageId);
        try
        {
            fileStorage.CreateDirectory(dir);
            await using var stream = new MemoryStream("dummy"u8.ToArray());
            await fileStorage.WriteAsync(Path.Combine(dir, $"{packageId}.dll"), stream, CancellationToken.None);
            _pluginManager.Registry.MarkInstalled(packageId, PluginSource.Zip, "1.0.0", DateTimeOffset.UtcNow);

            //Act
            var response = await client.Request(_apiUrl, "Uninstall")
                .AllowAnyHttpStatus()
                .PostJsonAsync(new UninstallPluginRequest { PackageId = packageId });

            //Assert: папка залочена загруженной сборкой — только отметка, удаление при рестарте
            response.StatusCode.Should().Be(StatusCodes.Status200OK);
            _pluginManager.Registry.Get(packageId)!.PendingDelete.Should().BeTrue();
            fileStorage.DirectoryExists(dir).Should().BeTrue();
        }
        finally
        {
            _pluginManager.Registry.Remove(packageId);
            _pluginManager.RemovePlugin(packageId);
            if (fileStorage.DirectoryExists(dir))
                fileStorage.DeleteDirectory(dir, recursive: true);
        }
    }

    [IntegrationFact]
    public async Task DisabledPlugin_StaysInList_AndCanBeReenabled()
    {
        //Arrange
        var client = AppFixture.GetClient();
        _fixture.AddTestPlugin(AppFixture.ServiceProvider);
        var packageId = _pluginManager.Plugins.First().Info.PackageId;
        try
        {
            _pluginManager.Registry.MarkInstalled(packageId, PluginSource.Zip, "1.0.0", DateTimeOffset.UtcNow);

            var disable = await client.Request(_apiUrl, "SetEnabled")
                .AllowAnyHttpStatus()
                .PostJsonAsync(new SetPluginEnabledRequest { PackageId = packageId, Enabled = false });
            disable.StatusCode.Should().Be(StatusCodes.Status200OK);

            // как после рестарта: отключённый плагин не загрузился
            _pluginManager.RemovePlugin(packageId);

            //Act
            var list = await client.Request(_apiUrl, "list/offset")
                .AppendQueryParam(new ListPluginQueryRequest { Take = 50 })
                .GetJsonAsync<ListDataResult<PluginInfoResponse>>();

            //Assert
            var item = list.Items.Single(i => i.PackageId == packageId);
            item.Enabled.Should().BeFalse();

            var enable = await client.Request(_apiUrl, "SetEnabled")
                .AllowAnyHttpStatus()
                .PostJsonAsync(new SetPluginEnabledRequest { PackageId = packageId, Enabled = true });
            enable.StatusCode.Should().Be(StatusCodes.Status200OK);
            _pluginManager.Registry.Get(packageId)!.Disabled.Should().BeFalse();
        }
        finally
        {
            _pluginManager.Registry.Remove(packageId);
            _pluginManager.RemovePlugin(packageId);
        }
    }
}
