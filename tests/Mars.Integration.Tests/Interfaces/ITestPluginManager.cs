using Mars.Plugin.Dto;
using Mars.Plugin.Services;
using Microsoft.Extensions.Configuration;

namespace Mars.Integration.Tests.Interfaces;

public interface IPluginManagerWrapperForTests
{
    public IReadOnlyCollection<PluginData> Plugins { get; }
    void ApplyPluginMigrations(IServiceProvider rootServices, IConfiguration configuration);
}

internal class PluginManagerWrapperForTests : IPluginManagerWrapperForTests
{
    private readonly PluginManager _pluginManager;

    public PluginManagerWrapperForTests(PluginManager pluginManager)
    {
        _pluginManager = pluginManager;
    }

    public IReadOnlyCollection<PluginData> Plugins => _pluginManager.Plugins;

    public void ApplyPluginMigrations(IServiceProvider rootServices, IConfiguration configuration)
        => _pluginManager.ApplyPluginMigrations(rootServices, configuration);

}
