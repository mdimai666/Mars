using Mars.Plugin.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Plugin;

public static class ApplicationPluginExtensions
{
    static List<PluginData> _plugins = new();

    public static List<PluginData> Plugins => _plugins;

    public static IEnumerable<string> ScanFolderForPlugins(string pluginsPath)
    {
        List<string> list = new();

        var dirs = Directory.EnumerateDirectories(pluginsPath);

        foreach (var dir in dirs)
        {
            string folderName = dir.Split(Path.DirectorySeparatorChar).Last();
            if (!folderName.StartsWith('.') && !folderName.StartsWith('_'))
            {
                //    var rootFiles = Directory.EnumerateDirectories(dir, "*.dll");

                //    foreach (var file in rootFiles)
                //    {
                //        string fileNameWext = Path.GetFileNameWithoutExtension(file);

                //        if(filenam)

                //    }

                string expectDllName = folderName + ".dll";

                string expectPath = Path.Combine(dir, expectDllName);

                if (File.Exists(expectPath))
                {
                    list.Add(expectPath);
                }
            }
        }

        return list;
    }

    public static WebApplicationBuilder AddPlugins(this WebApplicationBuilder builder, string pluginSection = "Plugins")
    {
        var plugins = new List<PluginData>();

        var pluginsSection = builder.Configuration.GetSection(pluginSection);

        if (pluginsSection is null)
        {
            return builder;
        }

        var pluginConfig = new Dictionary<string, PluginConfig>();
        pluginsSection.Bind(pluginConfig);

        foreach (var (name, c) in pluginConfig)
        {
            if (name.StartsWith('_')) continue;

            var assemblyFile = Path.GetFullPath(c.AssemblyPath);
            var contentRootPath = c.ContentRootPath is not null ? Path.GetFullPath(c.ContentRootPath) : null;

            var settings = new PluginSettings { ContentRootPath = contentRootPath ?? Path.GetDirectoryName(assemblyFile)! };

            var currentAssembly = Assembly.LoadFrom(assemblyFile);

            foreach (var attr in currentAssembly.GetCustomAttributes<WebApplicationPluginAttribute>())
            {
                var type = attr.PluginType;

                // Detect if those methods were overridden
                var doBuilder = type.GetMethod(nameof(WebApplicationPlugin.ConfigureWebApplicationBuilder))?.DeclaringType != typeof(WebApplicationPlugin);
                var doApp = type.GetMethod(nameof(WebApplicationPlugin.ConfigureWebApplication))?.DeclaringType != typeof(WebApplicationPlugin);

                //currentAssembly.CustomAttributes

                PluginInfo info = new PluginInfo(currentAssembly);

                // This type isn't instantiated using DI (chicken and egg problem)
                plugins.Add(new(doBuilder, doApp, settings, (WebApplicationPlugin)Activator.CreateInstance(type)!, info));
            }

        }

        foreach (var p in plugins)
        {
            if (p.ConfigureWebApplicationBuilder)
            {
                p.Plugin.ConfigureWebApplicationBuilder(builder, p.Settings);

                // Use the same instance when mapping plugins
                builder.Services.AddSingleton(typeof(PluginData), p);
            }
        }

        _plugins = plugins;
        return builder;
    }

    public static void UsePlugins(this WebApplication app)
    {
        foreach (var p in app.Services.GetServices<PluginData>())
        {
            if (p.ConfigureWebApplication)
            {
                p.Plugin.ConfigureWebApplication(app, p.Settings);
            }
        }

        if (_plugins.Count > 0)
        {
            Console.WriteLine("========================");
            Console.WriteLine("Plugins: ");
            foreach (var plugin in _plugins)
            {
                Console.WriteLine($" + {plugin.Info.Title} (v{plugin.Info.Version}) ");
                Console.WriteLine($"   [{Path.GetRelativePath(Directory.GetCurrentDirectory(), plugin.Info.AssemblyPath)}]");
            }
            Console.WriteLine();
        }
    }

    public static IMvcBuilder AddPluginsAsPartOfMvc(this IMvcBuilder mvcBuilder)
    {
        //var plugins = mvcBuilder.Services.Where(s => s.ServiceType == typeof(PluginData));

        foreach (var p in _plugins)
        {
            var assembly = p.Plugin.GetType().Assembly;
            mvcBuilder.PartManager.ApplicationParts.Add(new AssemblyPart(assembly));
        }

        return mvcBuilder;
    }

    public record PluginData(bool ConfigureWebApplicationBuilder,
                      bool ConfigureWebApplication,
                      PluginSettings Settings,
                      WebApplicationPlugin Plugin, PluginInfo Info);

    internal class PluginConfig
    {
        public string? ContentRootPath { get; set; }
        public string AssemblyPath { get; set; } = default!;
    }

}
