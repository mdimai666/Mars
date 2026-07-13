using System.Reflection;
using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.Services;
using Mars.Plugin.Abstractions;
using Mars.Plugin.Dto;
using Mars.Plugin.Front.Abstractions;
using Mars.Plugin.PluginProvider.Providers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<PluginManager>();

        var dataDirHostingInfo = MOptions.Create(new FileHostingInfo()
        {
            Backend = null,
            PhysicalPath = new Uri(Path.Combine(contentRootPath, "data"), UriKind.Absolute),
            RequestPath = ""
        });
        _fileStorage = new FileStorage(dataDirHostingInfo);
        isTesting = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Test", StringComparison.OrdinalIgnoreCase) ?? false;

        _logger.LogDebug("PluginManager initialized. ContentRoot: {ContentRootPath}, IsTesting: {IsTesting}", contentRootPath, isTesting);
        EnsurePluginsDirExist();
    }

    void EnsurePluginsDirExist()
    {
        if (!_fileStorage.DirectoryExists(PluginsDefaultPath))
        {
            _logger.LogDebug("Plugins directory not found, creating: {Path}", PluginsDefaultPath);
            _fileStorage.CreateDirectory(PluginsDefaultPath);
        }
    }

    internal void ConfigureBuilder(WebApplicationBuilder builder, string pluginSection = "Plugins")
    {
        _logger.LogInformation("=== Starting plugins configuration ===");

        var plugins = new List<PluginData>();
        var pluginsSection = builder.Configuration.GetSection(pluginSection);

        if (pluginsSection is null)
        {
            _logger.LogInformation("Section '{Section}' not found in configuration.", pluginSection);
            return;
        }

        // Read from appsettings.json
        var pluginConfigureDefinition = new Dictionary<string, PluginConfig>();
        pluginsSection.Bind(pluginConfigureDefinition);

        _logger.LogDebug("Found {Count} plugins in configuration section '{Section}'.", pluginConfigureDefinition.Count, pluginSection);
        foreach (var (name, pluginConfig) in pluginConfigureDefinition)
        {
            if (name.StartsWith('_'))
            {
                _logger.LogDebug("Plugin '{Name}' skipped (starts with '_').", name);
                continue;
            }

            _logger.LogDebug("Processing plugin from configuration: {Name}", name);
            var instances = InstatitePlugin(pluginConfig, _logger);
            plugins.AddRange(instances);
        }

        // Read from /data/plugins dir
        if (!isTesting)
        {
            _logger.LogDebug("Scanning directory '{Dir}' for plugins...", PluginsDefaultPath);
            foreach (var pluginConfig in ReadPluginsFromDirectory(_fileStorage, PluginsDefaultPath, _logger))
            {
                try
                {
                    var instances = InstatitePlugin(pluginConfig, _logger);
                    plugins.AddRange(instances);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Critical error during plugin initialization: {AssemblyPath}", pluginConfig.AssemblyPath);
                }
            }
        }

        _logger.LogInformation("Total {Count} plugin instances loaded. Calling ConfigureWebApplicationBuilder...", plugins.Count);

        foreach (var p in plugins)
        {
            if (p.hasConfigureWebApplicationBuilder)
            {
                try
                {
                    _logger.LogDebug("Calling ConfigureWebApplicationBuilder for {PluginName}", p.Info.KeyName);
                    p.Plugin.ConfigureWebApplicationBuilder(builder, p.Settings);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in ConfigureWebApplicationBuilder of plugin: {PluginName}", p.Info.KeyName);
                }
            }
            else
            {
                _logger.LogDebug("Plugin {PluginName} does not override ConfigureWebApplicationBuilder.", p.Info.KeyName);
            }
        }

        _logger.LogInformation("=== Plugins configuration completed. Active: {Count} ===", plugins.Count);
        _plugins = plugins;
    }

    internal void ApplyPluginMigrations(IServiceProvider rootServices, IConfiguration configuration)
    {
        _logger.LogInformation("=== Applying plugin migrations ===");
        foreach (var pluginData in _plugins)
        {
            if (pluginData.Plugin is IPluginDatabaseMigrator migrator)
            {
                _logger.LogInformation("Applying migrations for plugin: {PluginName}", pluginData.Info.KeyName);
                try
                {
                    migrator.ApplyMigrations(rootServices, configuration, pluginData.Settings)
                            .ConfigureAwait(false).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error applying migrations for {PluginName}", pluginData.Info.KeyName);
                }
            }
        }
    }

    internal void UsePlugins(WebApplication app)
    {
        _logger.LogInformation("=== Registering plugins in request pipeline (UsePlugins) ===");
        foreach (var pluginData in _plugins)
        {
            if (pluginData.hasConfigureWebApplication)
            {
                try
                {
                    _logger.LogDebug("Calling ConfigureWebApplication for {PluginName}", pluginData.Info.KeyName);
                    pluginData.Plugin.ConfigureWebApplication(app, pluginData.Settings);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in ConfigureWebApplication of plugin: {PluginName}", pluginData.Info.KeyName);
                }
            }

            var pluginWwwRoot = Path.Combine(pluginData.Settings.ContentRootPath, "wwwroot");

            var manifestProvider = new PluginManifestProvider(pluginData.Plugin.GetType().Assembly);

            var pluginUrl = $"/_plugin/{pluginData.Info.KeyName}";
            app.Map(pluginUrl, pluginAppBuilder =>
            {
                pluginAppBuilder.UseRouting();

                if (manifestProvider.Files.Any())
                {
                    var manifest = manifestProvider.GenerateManifest(app, pluginData, _logger);
                    var pluginManifestUrl = $"{pluginUrl}/{MarsFrontPluginManifest.DefaultManifestFileName}";

                    pluginAppBuilder.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/" + MarsFrontPluginManifest.DefaultManifestFileName, () => Results.Json(manifest));
                        _logger.LogInformation("Serving ManifestFile for {PluginName} at {Url}, Files={Files}", pluginData.Info.KeyName, pluginManifestUrl, manifestProvider.Files.Count);

                        endpoints.MapGet("/health", () => TypedResults.Text("OK"));
                    });
                }

                if (Directory.Exists(pluginWwwRoot))
                {
                    pluginAppBuilder.UseStaticFiles(new StaticFileOptions
                    {
                        ServeUnknownFileTypes = true,
                        FileProvider = new PhysicalFileProvider(pluginWwwRoot),
                    });
                    _logger.LogInformation("Serving static files for {PluginName} at {Url}", pluginData.Info.KeyName, pluginUrl);

                }

                //В режиме Debug надо сервить wwwroot из плагинов
                var pluginMainAssembly = pluginData.Info.Assembly;
                ServePluginSubProjectsWwwRoot(pluginMainAssembly, pluginAppBuilder);

            });
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

    internal static List<PluginData> InstatitePlugin(PluginConfig pluginConfig, ILogger logger)
    {
        var result = new List<PluginData>();
        var assemblyFile = Path.GetFullPath(pluginConfig.AssemblyPath);
        var contentRootPath = pluginConfig.ContentRootPath is not null ? Path.GetFullPath(pluginConfig.ContentRootPath) : null;

        var settings = new PluginSettings { ContentRootPath = contentRootPath ?? Path.GetDirectoryName(assemblyFile)! };

        logger.LogDebug("Attempting to load plugin assembly: {AssemblyPath}", assemblyFile);

        Assembly currentAssembly;
        try
        {
            currentAssembly = Assembly.LoadFrom(assemblyFile);
            logger.LogDebug("Assembly {AssemblyName} successfully loaded into context.", currentAssembly.FullName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load assembly at path: {AssemblyPath}. Check dependencies and paths.", assemblyFile);
            return result;
        }

        var attributes = currentAssembly.GetCustomAttributes<WebApplicationPluginAttribute>().ToList();

        if (attributes.Count == 0)
        {
            logger.LogWarning("NO [WebApplicationPluginAttribute] found in assembly {Assembly}! Plugin will not be loaded.", assemblyFile);
            return result;
        }

        foreach (var attr in attributes)
        {
            var type = attr.PluginType;
            logger.LogDebug("Found plugin attribute. Type: {PluginType}", type.FullName);

            var hasConfigureBuilder = type.GetMethod(nameof(WebApplicationPlugin.ConfigureWebApplicationBuilder))?.DeclaringType != typeof(WebApplicationPlugin);
            var hasConfigureApp = type.GetMethod(nameof(WebApplicationPlugin.ConfigureWebApplication))?.DeclaringType != typeof(WebApplicationPlugin);

            PluginInfo info = new(currentAssembly);

            try
            {
                var instance = (WebApplicationPlugin)Activator.CreateInstance(type)!;
                logger.LogInformation("Plugin {PluginType} successfully instantiated. Methods overridden: Builder={HasBuilder}, App={HasApp}",
                    type.Name, hasConfigureBuilder, hasConfigureApp);

                result.Add(new PluginData(hasConfigureBuilder, hasConfigureApp, settings, instance, info));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Critical error creating instance (Activator.CreateInstance) for type {PluginType}. Ensure the class has a public parameterless constructor.", type.FullName);
            }
        }

        return result;
    }

    internal IEnumerable<PluginConfig> ReadPluginsFromDirectory(IFileStorage fileStorage, string dir, ILogger logger)
    {
        logger.LogDebug("Reading directory contents: {Dir}", dir);
        var dirs = fileStorage.GetDirectoryContents(dir);

        foreach (var pluginDir in dirs.Where(s => s.IsDirectory))
        {
            if (pluginDir.Name.StartsWith('_'))
            {
                logger.LogDebug("Directory {DirName} skipped (starts with '_').", pluginDir.Name);
                continue;
            }

            logger.LogDebug("Found potential plugin folder: {PluginDir}", pluginDir.Name);
            var path = Path.Combine(dir, pluginDir.Name);
            var pluginRootFiles = fileStorage.GetDirectoryContents(path);

            var runtimeFiles = pluginRootFiles.Where(s => s.Name.EndsWith(".runtimeconfig.json") && !s.Name.EndsWith(".dev.runtimeconfig.json")).ToList();

            if (!runtimeFiles.Any())
            {
                logger.LogDebug("Plugin folder {PluginDir} missing .runtimeconfig.json file. Skipping.", pluginDir.PhysicalPath);
                continue;
            }

            if (runtimeFiles.Count() > 1)
            {
                var dirNameRuntime = runtimeFiles.FirstOrDefault(s => s.Name == $"{pluginDir.Name}.runtimeconfig.json");
                if (dirNameRuntime == null)
                {
                    logger.LogWarning("Multiple .runtimeconfig.json files found in {PluginDir}, but none matches folder name. Skipping.", pluginDir.PhysicalPath);
                    continue;
                }
                runtimeFiles = [dirNameRuntime];
            }

            var runtimeFile = runtimeFiles.First();
            var dllFilePath = runtimeFile.PhysicalPath.Replace(".runtimeconfig.json", ".dll");
            var dllDir = Path.GetDirectoryName(dllFilePath);

            if (!File.Exists(dllFilePath))
            {
                logger.LogWarning("Plugin DLL file not found: {DllFilePath}. Expected next to {RuntimeFile}", dllFilePath, runtimeFile.Name);
                continue;
            }

            logger.LogDebug("Found valid plugin in directory: {DllFilePath}", dllFilePath);
            yield return new PluginConfig
            {
                AssemblyPath = dllFilePath,
                ContentRootPath = dllDir,
            };
        }
    }

    internal void ServePluginSubProjectsWwwRoot(Assembly pluginMainAssembly, IApplicationBuilder pluginAppBuilder)
    {
        if (PluginAssemblyHelper.IsAssemblyDebugBuild(pluginMainAssembly))
        {
            var projectAssemblies = PluginAssemblyHelper.ReadFrontAssemblies(pluginMainAssembly);
            foreach (var assembly in projectAssemblies)
            {
                var frontAssemblyName = assembly.GetName().Name!;
                string targetFrameworkName = $"net{Environment.Version.Major}.{Environment.Version.Minor}";

                var projectPath = assembly.Location.Split("\\bin\\", 2)[0];
                var frontDir = new DirectoryInfo(Path.Combine(projectPath, "..", frontAssemblyName));
                var frontWwwRoot = Path.Combine(frontDir.FullName, "wwwroot");
                var frontBinWwwRoot = Path.Combine(frontDir.FullName, "bin", "Debug", targetFrameworkName, "wwwroot");

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
        }
    }
}
