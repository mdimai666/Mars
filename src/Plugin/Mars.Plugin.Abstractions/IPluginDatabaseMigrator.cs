using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Mars.Plugin.Abstractions;

public interface IPluginDatabaseMigrator
{
    Task ApplyMigrations(IServiceProvider rootServices, IConfiguration configuration, PluginSettings settings);
}
