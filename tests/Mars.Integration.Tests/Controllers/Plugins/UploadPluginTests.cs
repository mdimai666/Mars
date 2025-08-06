using System.Text;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Options.Models;
using Mars.Plugin;
using Mars.Plugin.Services;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Plugins;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Test.Mars.Plugin.Extensions;

namespace Mars.Integration.Tests.Controllers.Plugins;

public class UploadPluginTests : ApplicationTests
{
    const string _apiUrl = "/api/Plugin/UploadPlugin";
    private readonly IPluginService _pluginService;
    private readonly IOptionService _optionService;
    private readonly IFileStorage _fileStorage;

    public UploadPluginTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _pluginService = appFixture.ServiceProvider.GetRequiredService<IPluginService>();
        _optionService = appFixture.ServiceProvider.GetRequiredService<IOptionService>();
        SetAllowUploadZipManually(true);
        //[FromKeyedServices("data")]
        _fileStorage = appFixture.ServiceProvider.GetRequiredKeyedService<IFileStorage>("data");
    }

    void SetAllowUploadZipManually(bool allowUploadZipManually)
    {
        var opt = _optionService.GetOption<PluginManagerSettingsOption>();
        opt.AllowUploadZipManually = allowUploadZipManually;
        _optionService.SetOptionOnMemory(opt);
    }

    [IntegrationFact]
    public async Task UploadPlugin_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(PluginController.UploadPlugin);
        _ = nameof(IPluginService.UploadPlugin);
        var client = AppFixture.GetClient(true);

        //Act
        var result = await client.Request(_apiUrl).AllowAnyHttpStatus().PostAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task UploadPlugin_Request_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PluginController.UploadPlugin);
        _ = nameof(PluginService.UploadPlugin);
        var client = AppFixture.GetClient();
        var pluginName = "Project.Plugin1";
        var manifest = NuspecExtension.MockManifest();

        var assemblyBin = PluginAssemblyGenerator.GeneratePluginAssembly(pluginName, "MyPlugin", "1.0.1.0", new()
        {
            ["PackageId"] = manifest.PackageId,
        });

        using var zip = ZipHelper.ZipFiles(new()
        {
            [pluginName + ".dll"] = assemblyBin,
            [pluginName + ".nuspec"] = Encoding.UTF8.GetBytes(NuspecHelper.CreateNuspec(manifest, "0.6.5-alpha.1")),
            [pluginName + ".runtimeconfig.json"] = Encoding.UTF8.GetBytes("{}"),
        });

        //var pluginInfo = new PluginInfo(Assembly.Load(assemblyBin));

        //Act
        var result = await client.Request(_apiUrl)
                                .PostMultipartAsync(mp => mp
                                    .AddFile("files", zip, "plugin1.zip", "application/zip")
                                )
                                .CatchUserActionError()
                                .ReceiveJson<PluginsUploadOperationResultResponse>();

        //Assert
        result.Should().NotBeNull();
        result.Items.Count().Should().Be(1);

        var item = result.Items.First();
        item.Success.Should().BeTrue();
        item.FileName.Should().Be("plugin1.zip");
        item.FileSize.Should().Be(zip.Length);
        item.ErrorMessage.Should().BeNullOrEmpty();

        var pluginDllFileName = Path.Combine(PluginService.PluginsDefaultPath, Path.GetFileNameWithoutExtension("plugin1.zip"), pluginName + ".dll");
        _fileStorage.FileExists(pluginDllFileName).Should().BeTrue("Plugin DLL file should be created in the plugins directory");
    }

    [IntegrationFact]
    public void UploadPlugin_BadContentType_Should400()
    {
        //Arrange
        _ = nameof(PluginController.UploadPlugin);
        _ = nameof(PluginService.UploadPlugin);
        var client = AppFixture.GetClient();

        //Act
        var action = () => client.Request(_apiUrl)
                                .PostMultipartAsync(mp => mp
                                    .AddFile("files", GenerateStreamFromString("zzz"), "plugin1.zip", "application/NON_zip_FILE")
                                )
                                .ReceiveJson<PluginsUploadOperationResultResponse>();

        //Assert
        action.Should().ThrowAsync<FlurlHttpException>()
            .RunSync().Which.GetResponseJsonAsync<ValidationProblemDetails>().RunSync()
            .Detail.Should().Match("*Only ZIP files are allowed.*");
    }

    [IntegrationFact]
    public void UploadPlugin_OnDisallowUploadZipManually_Should466Denied()
    {
        //Arrange
        SetAllowUploadZipManually(false);
        _ = nameof(PluginController.UploadPlugin);
        _ = nameof(PluginService.UploadPlugin);
        var client = AppFixture.GetClient();

        //Act
        var action = () => client.Request(_apiUrl)
                                .PostMultipartAsync(mp => mp
                                    .AddFile("files", GenerateStreamFromString("zzz"), "plugin1.zip", "application/zip")
                                )
                                .ReceiveJson<PluginsUploadOperationResultResponse>();

        //Assert
        action.Should().ThrowAsync<FlurlHttpException>()
            .RunSync().Which.GetResponseJsonAsync<UserActionResult>().RunSync()
            .Message.Should().Be(PluginService.ErrorNotAllowUploadZipManuallyMessage);
    }

    private MemoryStream GenerateStreamFromString(string value)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(value ?? ""));
    }
}
