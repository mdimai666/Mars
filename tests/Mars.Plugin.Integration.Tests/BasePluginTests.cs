using System.Text;
using System.Text.Json;
using AutoFixture;
using Mars.Host.Data.Contexts;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Plugin.Integration.Tests.Tests;
using Microsoft.Extensions.Configuration;
using PluginExample.Data;
using PluginExample.Data.Seeds;
using static Mars.Plugin.ApplicationPluginExtensions;

namespace Mars.Plugin.Integration.Tests;

[CollectionDefinition("PluginTestApp")]
public class TestPluginAppCollection : ICollectionFixture<PluginApplicationFixture>
{

}

public class PluginApplicationFixture : ApplicationFixture
{
    internal Dictionary<string, PluginConfig> _pluginConfigs = default!;

    protected override void ModifyConfigurationBuilder(IConfigurationBuilder builder)
    {
        var pluginsConfig = GetPluginConfigs();
        var json = JsonSerializer.Serialize(new
        {
            Plugins = pluginsConfig
        });
        _pluginConfigs = pluginsConfig;
        builder.AddJsonStream(new MemoryStream(Encoding.ASCII.GetBytes(json)));
    }

    internal Dictionary<string, PluginConfig> GetPluginConfigs()
    {
        var pluginDllDir = PluginFilesTests.PluginDllPath();

        if (!File.Exists(pluginDllDir)) throw new ArgumentException("Plugin .Dll path not valid!");

        return new()
        {
            ["PluginExample"] = new()
            {
                AssemblyPath = pluginDllDir,
                ContentRootPath = Path.GetDirectoryName(pluginDllDir),
            }
        };
    }
}

[Collection("PluginTestApp")]
public abstract class BasePluginTests
{

    protected readonly PluginApplicationFixture AppFixture;
    protected MarsDbContext DbContext => AppFixture.DbFixture.DbContext;

    public IFixture _fixture = new Fixture();

    protected BasePluginTests(PluginApplicationFixture appFixture)
    {
        AppFixture = appFixture;
        AppFixture.DbFixture.Reset().RunSync();
        AppFixture.Seed().RunSync();
        AppFixture.ResetMocks();
        //AppFixture.MessageQueueFixture.ClearTopics().RunSync();
        PluginSeed();
    }

    public void PluginSeed()
    {
        var conn0 = AppFixture.Configuration.GetConnectionString("DefaultConnection")!;
        var ef = MyPluginDbContext.CreateInstance(conn0);
        PluginNewsSeed.SeedFirstData(ef, AppFixture.ServiceProvider, AppFixture.Configuration, "").Wait();
    }
}
