using System.Text;
using System.Text.Json;
using Mars.Core.Models;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Common;
using Mars.UseStartup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Mars.AppFrontEngines.Integration.Tests.DatabaseHandlebarsEngine;

public class DatabaseHandlebarsAppFrontApplicationFixture : ApplicationFixture
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

        return [
            new AppFrontSettingsCfg(){
                Mode = AppFrontMode.HandlebarsTemplate,
                Path = "",
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
