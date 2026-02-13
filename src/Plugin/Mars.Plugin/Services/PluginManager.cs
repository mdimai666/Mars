using System.Reflection;
using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.Services;
using Mars.Plugin.Abstractions;
using Mars.Plugin.Dto;
using Mars.Plugin.PluginProvider.Providers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using MOptions = Microsoft.Extensions.Options.Options;

namespace Mars.Plugin.Services;

internal class PluginManager
{
    private List<PluginData> _plugins = [];
    private readonly IFileStorage _fileStorage;
    private readonly bool isTesting;
    private readonly ILogger<PluginManager> _logger;

    public IReadOnlyCollection<PluginData> Plugins => _plugins;
    public const string PluginsDefaultPath = "plugins";

    public PluginManager(string contentRootPath)
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });
        _logger = loggerFactory.CreateLogger<PluginManager>();

        var dataDirHostingInfo = MOptions.Create(new FileHostingInfo()
        {
            Backend = null,
            PhysicalPath = new Uri(Path.Combine(contentRootPath, "data"), UriKind.Absolute),
            RequestPath = ""
        });
        _fileStorage = new FileStorage(dataDirHostingInfo);
        isTesting = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Test", StringComparison.OrdinalIgnoreCase) ?? false;

        EnsurePluginsDirExist();
    }

    void EnsurePluginsDirExist()
    {
        if (!_fileStorage.DirectoryExists(PluginsDefaultPath)) _fileStorage.CreateDirectory(PluginsDefaultPath);
    }

    internal void ConfigureBuilder(WebApplicationBuilder builder, string pluginSection = "Plugins")
    {
        //if (isTesting) return;
        var plugins = new List<PluginData>();

        var pluginsSection = builder.Configuration.GetSection(pluginSection);

        if (pluginsSection is null)
        {
            return;
        }

        // Read from appsettings.json
        var pluginConfigureDefinition = new Dictionary<string, PluginConfig>();
        pluginsSection.Bind(pluginConfigureDefinition);

        foreach (var (name, pluginConfig) in pluginConfigureDefinition)
        {
            if (name.StartsWith('_')) continue;
            var instances = InstatitePlugin(pluginConfig);
            plugins.AddRange(instances);
        }

        // Read from /data/plugins dir
        if (!isTesting)
        {
            foreach (var pluginConfig in ReadPluginsFromDirectory(_fileStorage, PluginsDefaultPath, _logger))
            {
                var instances = InstatitePlugin(pluginConfig);
                plugins.AddRange(instances);
            }
        }

        foreach (var p in plugins)
        {
            if (p.hasConfigureWebApplicationBuilder)
            {
                p.Plugin.ConfigureWebApplicationBuilder(builder, p.Settings);
            }
        }

        _plugins = plugins;
    }

    internal void ApplyPluginMigrations(IServiceProvider rootServices, IConfiguration configuration)
    {
        //if (isTesting) return;
        foreach (var pluginData in _plugins)
        {
            if (pluginData.Plugin is IPluginDatabaseMigrator migrator)
            {
                migrator.ApplyMigrations(rootServices, configuration, pluginData.Settings)
                        .ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }
    }

    internal void UsePlugins(WebApplication app)
    {
        //if (isTesting) return;
        foreach (var pluginData in _plugins)
        {
            if (pluginData.hasConfigureWebApplication)
            {
                pluginData.Plugin.ConfigureWebApplication(app, pluginData.Settings);
            }

            var pluginWwwRoot = Path.Combine(pluginData.Settings.ContentRootPath, "wwwroot");

            var manifestProvider = new PluginManifestProvider(pluginData.Plugin.GetType().Assembly);
            manifestProvider.ProvideManifest(app, pluginData);

            if (Directory.Exists(pluginWwwRoot))
            {
                var pluginUrl = $"/_plugin/{pluginData.Info.KeyName}";
                app.Map(pluginUrl, pluginAppBuilder =>
                {
                    pluginAppBuilder.UseStaticFiles(new StaticFileOptions
                    {
                        ServeUnknownFileTypes = true,
                        FileProvider = new PhysicalFileProvider(pluginWwwRoot),
                    });
                });
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

    internal void AddPlugin(PluginData pluginData) => _plugins.Add(pluginData);

    internal static IEnumerable<PluginData> InstatitePlugin(PluginConfig pluginConfig)
    {
        var assemblyFile = Path.GetFullPath(pluginConfig.AssemblyPath);
        var contentRootPath = pluginConfig.ContentRootPath is not null ? Path.GetFullPath(pluginConfig.ContentRootPath) : null;

        var settings = new PluginSettings { ContentRootPath = contentRootPath ?? Path.GetDirectoryName(assemblyFile)! };

        var currentAssembly = Assembly.LoadFrom(assemblyFile);

        foreach (var attr in currentAssembly.GetCustomAttributes<WebApplicationPluginAttribute>())
        {
            var type = attr.PluginType;

            // Detect if those methods were overridden
            var hasConfigureBuilder = type.GetMethod(nameof(WebApplicationPlugin.ConfigureWebApplicationBuilder))?.DeclaringType != typeof(WebApplicationPlugin);
            var hasConfigureApp = type.GetMethod(nameof(WebApplicationPlugin.ConfigureWebApplication))?.DeclaringType != typeof(WebApplicationPlugin);

            //currentAssembly.CustomAttributes

            PluginInfo info = new(currentAssembly);

            // This type isn't instantiated using DI (chicken and egg problem)
            yield return new PluginData(hasConfigureBuilder, hasConfigureApp, settings, (WebApplicationPlugin)Activator.CreateInstance(type)!, info);
        }
    }

    internal IEnumerable<PluginConfig> ReadPluginsFromDirectory(IFileStorage fileStorage, string dir, ILogger logger)
    {
        var dirs = fileStorage.GetDirectoryContents(dir);

        foreach (var pluginDir in dirs.Where(s => s.IsDirectory))
        {
            if (pluginDir.Name.StartsWith('_')) continue;
            var path = Path.Combine(dir, pluginDir.Name);
            var pluginRootFiles = fileStorage.GetDirectoryContents(path);

            var runtimeFiles = pluginRootFiles.Where(s => s.Name.EndsWith(".runtimeconfig.json"));
            if (!runtimeFiles.Any())
            {
                logger.LogInformation("plugin directory must contain a .runtimeconfig.json file: {PluginDir}", pluginDir.PhysicalPath);
                continue;
            }
            if (runtimeFiles.Count() > 1)
            {
                logger.LogWarning("plugin directory contains multiple .runtimeconfig.json files: {PluginDir}", pluginDir.PhysicalPath);
                continue;
            }
            var runtimeFile = runtimeFiles.First();
            var dllFilePath = runtimeFile.PhysicalPath.Replace(".runtimeconfig.json", ".dll");
            var dllDir = Path.GetDirectoryName(dllFilePath);

            if (!File.Exists(dllFilePath))//нужен физический путь чтобы подгрузить.
            {
                logger.LogWarning("plugin dll file not found: {DllFilePath}, ({PhysicalPath})", dllFilePath, pluginDir.PhysicalPath);
                continue;
            }

            yield return new PluginConfig
            {
                AssemblyPath = dllFilePath,
                ContentRootPath = dllDir,
            };
        }
    }

}
