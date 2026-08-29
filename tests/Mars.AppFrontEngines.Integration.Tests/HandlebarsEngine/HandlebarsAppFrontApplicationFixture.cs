using System.Text;
using System.Text.Json;
using Mars.Core.Models;
using Mars.Integration.Tests.Common;
using Mars.SiteEngine;
using Mars.SiteEngine.Services;
using Mars.Test.Common.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Mars.AppFrontEngines.Integration.Tests.HandlebarsEngine;

public class HandlebarsAppFrontApplicationFixture : ApplicationFixture
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

    protected internal virtual AppFrontSettingsCfg[] GetAppFrontConfigs()
    {
        _ = nameof(MarsSiteEngineFrontStartup.UseMarsSiteEngineFront);

        var themeRoot = SolutionPathHelper.Resolve("tests", "Mars.AppFrontEngines.Integration.Tests", "HandlebarsEngine", "appTheme");

        return [
            new AppFrontSettingsCfg(){
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
