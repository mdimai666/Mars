using System.Reflection;
using System.Text;
using System.Text.Json;
using AutoFixture;
using Mars.Core.Models;
using Mars.Host.Data.Contexts;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.UseStartup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Mars.Integration.Tests.AppFront;

[CollectionDefinition("AppFrontTestApp")]
public class TestAppFrontAppCollection : ICollectionFixture<AppFrontApplicationFixture>
{

}

public class AppFrontApplicationFixture : ApplicationFixture
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

    internal AppFrontSettingsCfg[] GetAppFrontConfigs()
    {
        _ = nameof(StartupFront.AddFront);

        var testDirPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "..", "..", ".."));
        var wwwRoot = Path.Combine(testDirPath, "AppFronts", "appTheme");

        return [
            new AppFrontSettingsCfg(){
                Mode = AppFrontMode.HandlebarsTemplateStatic,
                Path = wwwRoot,
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

[Collection("AppFrontTestApp")]
public abstract class BaseAppFrontTests
{
    protected readonly AppFrontApplicationFixture AppFixture;
    protected MarsDbContext DbContext => AppFixture.DbFixture.DbContext;

    public IFixture _fixture = new Fixture();

    protected BaseAppFrontTests(AppFrontApplicationFixture appFixture)
    {
        AppFixture = appFixture;
        AppFixture.DbFixture.Reset().RunSync();
        AppFixture.Seed().RunSync();
        AppFixture.ResetMocks();
        //AppFixture.MessageQueueFixture.ClearTopics().RunSync();
    }

}
