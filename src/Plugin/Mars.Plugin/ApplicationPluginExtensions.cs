using Mars.Host.Shared.Services;
using Mars.Plugin.Dto;
using Mars.Plugin.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Plugin;

public static class ApplicationPluginExtensions
{
    private static readonly bool isTesting = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Test", StringComparison.OrdinalIgnoreCase) ?? false;

    public static WebApplicationBuilder AddPlugins(this WebApplicationBuilder builder)
    {
        var pluginManager = new PluginManager(builder.Environment.ContentRootPath);
        pluginManager.ConfigureBuilder(builder);
        builder.Services.AddSingleton(pluginManager);
        builder.Services.AddControllers().AddPluginsAsPartOfMvc(pluginManager.Plugins);

        builder.Services.AddSingleton<IPluginService, PluginService>();
        return builder;
    }

    public static void ApplyPluginMigrations(this WebApplication app)
    {
        if (isTesting) return;

        var pluginManager = app.Services.GetRequiredService<PluginManager>();
        pluginManager.ApplyPluginMigrations(app.Services, app.Configuration);
    }

    public static void UsePlugins(this WebApplication app)
    {
        var pluginManager = app.Services.GetRequiredService<PluginManager>();
        pluginManager.UsePlugins(app);
    }

    static IMvcBuilder AddPluginsAsPartOfMvc(this IMvcBuilder mvcBuilder, IEnumerable<PluginData> plugins)
    {
        foreach (var p in plugins)
        {
            var assembly = p.Plugin.GetType().Assembly;
            mvcBuilder.PartManager.ApplicationParts.Add(new AssemblyPart(assembly));
        }

        return mvcBuilder;
    }

}
