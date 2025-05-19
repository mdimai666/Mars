using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace Mars.Plugin.PluginHost;

public static class PluginHostExtensions
{
    /// <summary>
    /// Serve static files of the main plugin assembly in the /bin folder
    /// </summary>
    /// <param name="pluginAppBuilder"></param>
    /// <param name="mainAssembly"></param>
    public static void ServePluginManifest(IApplicationBuilder pluginAppBuilder, Assembly mainAssembly)
    {
        var pluginAssembly = Assembly.GetExecutingAssembly();
        var pluginAssemblyName = pluginAssembly.GetName().Name!;
        var pluginAssemblyPath = Path.GetDirectoryName(pluginAssembly.Location)!;

        var frontBinWwwRoot = Path.Combine(pluginAssemblyPath, "wwwroot");

        pluginAppBuilder.UseStaticFiles(new StaticFileOptions
        {
            ServeUnknownFileTypes = true,
            FileProvider = new PhysicalFileProvider(frontBinWwwRoot),
        });
    }

    /// <summary>
    /// Serve static files, each of which depends on files for AppFront
    /// </summary>
    /// <param name="app"></param>
    /// <param name="mainAssembly"></param>
    /// <param name="frontAssemblies"></param>
    public static void UseDevelopingServePluginFilesDefinition(this WebApplication app, Assembly mainAssembly, Assembly[] frontAssemblies)
    {
        var pluginAssemblyName = mainAssembly.GetName().Name!;

        var startPath = $"/_plugin/{pluginAssemblyName}";

        app.Map(startPath, pluginAppBuilder =>
        {
            pluginAppBuilder.UseRouting();
            pluginAppBuilder.UseEndpoints(endpoints =>
            {
                endpoints.MapGet($"/health", async context =>
                {
                    await context.Response.WriteAsync("OK");
                });
            });

            ServePluginManifest(pluginAppBuilder, mainAssembly);

            foreach (var assembly in frontAssemblies)
            {
                var frontAssemblyName = assembly.GetName().Name!;

                var projectPath = assembly.Location.Split("\\bin\\", 2)[0];
                var frontDir = new DirectoryInfo(Path.Combine(projectPath, "..", frontAssemblyName));
                var frontWwwRoot = Path.Combine(frontDir.FullName, "wwwroot");
                var frontBinWwwRoot = Path.Combine(frontDir.FullName, "bin", "Debug", "net9.0", "wwwroot");

                if (Directory.Exists(frontBinWwwRoot))
                {

                    pluginAppBuilder.UseStaticFiles(new StaticFileOptions
                    {
                        ServeUnknownFileTypes = true,
                        FileProvider = new PhysicalFileProvider(frontBinWwwRoot),
                    });
                    pluginAppBuilder.UseStaticFiles(new StaticFileOptions
                    {
                        ServeUnknownFileTypes = true,
                        FileProvider = new PhysicalFileProvider(frontWwwRoot),
                    });
                }
            }

        });
    }
}
