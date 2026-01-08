using System.Reflection;
using System.Text;
using System.Text.Json;
using Mars.Core.Models;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Common;
using Mars.UseStartup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Mars.AppFrontEngines.Integration.Tests.BlazorEngine;

public class BlazorAppFrontApplicationFixture : ApplicationFixture
{
    internal AppFrontSettingsCfg[] _appFrontConfigs = default!;

    internal WebFilesReadFilesystemService _webFilesReadFilesystemService = default!;

    protected override void ModifyConfigurationBuilder(IConfigurationBuilder builder)
    {
        var configs = GetAppFrontConfigs();
        var json = JsonSerializer.Serialize(new
        {
            AppFront = configs
        });
        _appFrontConfigs = configs;
        builder.AddJsonStream(new MemoryStream(Encoding.ASCII.GetBytes(json)));
    }

    protected virtual internal AppFrontSettingsCfg[] GetAppFrontConfigs()
    {
        _ = nameof(StartupFront.AddFront);

        var testDirPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "..", "..", ".."));
        var themeRoot = Path.Combine(testDirPath, ".." , "BlazorTemplateExample", "bin", "Debug", "net10.0", "subpath");

        return [
            new AppFrontSettingsCfg(){
                Mode = AppFrontMode.BlazorPrerender,
                Path = themeRoot,
                Url = ""
            }
        ];
    }

    protected override void ModifyConfigureTestServices(IServiceCollection services)
    {
        _webFilesReadFilesystemService = Substitute.ForPartsOf<WebFilesReadFilesystemService>();
        services.AddSingleton(_webFilesReadFilesystemService);
    }
}
