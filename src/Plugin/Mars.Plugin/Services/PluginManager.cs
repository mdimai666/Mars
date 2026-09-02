using System.Reflection;
using Mars.Plugin.Abstractions;
using Mars.Plugin.Abstractions.Dto.Plugins;
using Mars.Plugin.Dto;
using Mars.Plugin.Front.Abstractions;
using Mars.Plugin.Handlers;
using Mars.Plugin.PluginProvider.Providers;
using Mars.Server.Abstractions.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mars.Plugin.Services;

internal class PluginManager
{
    private List<LoadedPlugin> _plugins = [];
    private readonly IFileStorage _fileStorage;
    private readonly bool isTesting;
    private readonly ILogger<PluginManager> _logger;
    private readonly PluginRegistry _registry;

    public IReadOnlyCollection<LoadedPlugin> Plugins => _plugins;
    public const string PluginsDefaultPath = "plugins";

    internal PluginRegistry Registry => _registry;

    public PluginManager(ILogger<PluginManager> logger, IFileStorage dataFileStorage)
    {
        _logger = logger;
        _fileStorage = dataFileStorage;
        _registry = new PluginRegistry(dataFileStorage);
        isTesting = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Test", StringComparison.OrdinalIgnoreCase) ?? false;

        _logger.LogDebug("PluginManager initialized. IsTesting: {IsTesting}", isTesting);
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

    /// <summary>
    /// Применяет отложенные отметки реестра строго до скана папок: удаляет папки
    /// плагинов, отмеченных к удалению, и подменяет отмеченные к подмене папки
    /// стейджингом новой версии. К моменту вызова сборки плагинов ещё не загружены,
    /// поэтому файлы обычно не залочены; при неудаче отметка остаётся до следующего старта.
    /// </summary>
    internal void ApplyPendingOperations()
    {
        foreach (var (packageId, entry) in _registry.Entries.ToList())
        {
            if (entry.PendingStagingDir is not null)
            {
                if (!_fileStorage.DirectoryExists(entry.PendingStagingDir))
                {
                    _logger.LogError("Pending staging '{Dir}' for plugin '{PackageId}' not found — dropping the mark.", entry.PendingStagingDir, packageId);
                    _registry.ClearPendingMarks(packageId);
                    continue;
                }

                var finalDir = Path.Combine(PluginsDefaultPath, packageId);
                try
                {
                    if (_fileStorage.DirectoryExists(finalDir))
                        _fileStorage.DeleteDirectory(finalDir, recursive: true);

                    _fileStorage.MoveDirectory(entry.PendingStagingDir, finalDir);
                    _registry.ClearPendingMarks(packageId);
                    _logger.LogInformation("Pending replace applied for plugin '{PackageId}'.", packageId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Cannot apply pending replace for plugin '{PackageId}' — will retry on next start.", packageId);
                }
                continue;
            }

            if (entry.PendingDelete)
            {
                var dir = Path.Combine(PluginsDefaultPath, packageId);
                try
                {
                    if (_fileStorage.DirectoryExists(dir))
                        _fileStorage.DeleteDirectory(dir, recursive: true);

                    _registry.Remove(packageId);
                    _logger.LogInformation("Pending delete applied for plugin '{PackageId}'.", packageId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Cannot delete folder of plugin '{PackageId}' — will retry on next start.", packageId);
                }
            }
        }
    }

    internal void ConfigureBuilder(WebApplicationBuilder builder, string pluginSection = "Plugins")
    {
        _logger.LogInformation("=== Starting plugins configuration ===");

        // тестовое окружение определяется хостингом, а не переменной окружения процесса
        var isTestEnv = builder.Environment.EnvironmentName == "Test";

        // тестовый хост делит data-папку с dev-инстансом — чужие отметки не применяет
        if (!isTestEnv)
            ApplyPendingOperations();

        var plugins = new List<LoadedPlugin>();
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
            var instances = InstantiatePlugin(pluginConfig, _logger, isolate: !isTestEnv);
            foreach (var instance in instances)
                instance.Info.Source = Contracts.Plugins.PluginSource.Config;
            plugins.AddRange(instances);
        }

        // Read from /data/plugins dir
        if (!isTestEnv)
        {
            _logger.LogDebug("Scanning directory '{Dir}' for plugins...", PluginsDefaultPath);
            foreach (var pluginConfig in ReadPluginsFromDirectory(_fileStorage, PluginsDefaultPath, _logger))
            {
                try
                {
                    var instances = InstantiatePlugin(pluginConfig, _logger, isolate: !isTestEnv);
                    foreach (var instance in instances)
                    {
                        var entry = _registry.Get(instance.Info.PackageId);
                        if (entry?.Disabled == true)
                        {
                            _logger.LogInformation("Plugin '{PackageId}' is disabled in registry — skipping.", instance.Info.PackageId);
                            continue;
                        }
                        if (entry?.PendingDelete == true)
                        {
                            _logger.LogInformation("Plugin '{PackageId}' is marked for deletion in registry — skipping.", instance.Info.PackageId);
                            continue;
                        }

                        instance.Info.Source = entry?.Source ?? Contracts.Plugins.PluginSource.Zip;
                        instance.Info.InstalledAt = entry?.InstalledAtUtc ?? DateTimeOffset.MinValue;
                        plugins.Add(instance);
                    }
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

            var manifestProvider = new PluginManifestProvider(pluginData.Plugin.GetType().Assembly, pluginData.Settings.ContentRootPath);

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
                        endpoints.MapGet("/" + MarsFrontPluginManifest.DefaultManifestFileName, () => Results.Json(manifest))
                            .ExcludeFromDescription();
                        _logger.LogInformation("Serving ManifestFile for {PluginName} at {Url}, Files={Files}", pluginData.Info.KeyName, pluginManifestUrl, manifestProvider.Files.Count);

                        endpoints.MapGet("/health", () => TypedResults.Text("OK"))
                            .ExcludeFromDescription();
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

    internal void AddPlugin(LoadedPlugin pluginData) => _plugins.Add(pluginData);

    internal void RemovePlugin(string packageId) => _plugins.RemoveAll(p => p.Info.PackageId == packageId);

    internal static List<LoadedPlugin> InstantiatePlugin(PluginConfig pluginConfig, ILogger logger, bool isolate = true)
    {
        var result = new List<LoadedPlugin>();
        var assemblyFile = Path.GetFullPath(pluginConfig.AssemblyPath);
        var contentRootPath = pluginConfig.ContentRootPath is not null ? Path.GetFullPath(pluginConfig.ContentRootPath) : null;

        var settings = new PluginSettings { ContentRootPath = contentRootPath ?? Path.GetDirectoryName(assemblyFile)! };

        logger.LogDebug("Attempting to load plugin assembly: {AssemblyPath}", assemblyFile);

        Assembly currentAssembly;
        try
        {
            if (isolate)
            {
                // изолированный контекст: свои зависимости у плагина, сборки Марса — из хоста
                var loadContext = new PluginLoadContext(assemblyFile);
                currentAssembly = loadContext.LoadFromAssemblyPath(assemblyFile);
                logger.LogDebug("Assembly {AssemblyName} successfully loaded into plugin load context.", currentAssembly.FullName);
            }
            else
            {
                // тестовое окружение: тип-идентичность с тестами важнее изоляции
                currentAssembly = Assembly.LoadFrom(assemblyFile);
                logger.LogDebug("Assembly {AssemblyName} successfully loaded into default context.", currentAssembly.FullName);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load assembly at path: {AssemblyPath}. Check dependencies and paths.", assemblyFile);
            return result;
        }

        var attributes = currentAssembly.GetCustomAttributes<MarsPluginAttribute>().ToList();

        if (attributes.Count == 0)
        {
            logger.LogWarning("NO [MarsPluginAttribute] found in assembly {Assembly}! Plugin will not be loaded.", assemblyFile);
            return result;
        }

        foreach (var attr in attributes)
        {
            var type = attr.PluginType;
            logger.LogDebug("Found plugin attribute. Type: {PluginType}", type.FullName);

            var hasConfigureBuilder = type.GetMethod(nameof(MarsPlugin.ConfigureWebApplicationBuilder))?.DeclaringType != typeof(MarsPlugin);
            var hasConfigureApp = type.GetMethod(nameof(MarsPlugin.ConfigureWebApplication))?.DeclaringType != typeof(MarsPlugin);

            PluginInfo info = new(currentAssembly);

            // дескриптор установленного плагина — источник отображаемых метаданных;
            // атрибуты сборки (из PluginInfo(Assembly)) — только фолбэк.
            var descriptor = PluginDescriptorHelper.TryRead(Path.Combine(settings.ContentRootPath, PluginPackageDescriptor.FileName));
            if (descriptor is not null)
            {
                if (!string.IsNullOrWhiteSpace(descriptor.Title)) info.Title = descriptor.Title!;
                if (!string.IsNullOrWhiteSpace(descriptor.Description)) info.Description = descriptor.Description!;
                if (!string.IsNullOrWhiteSpace(descriptor.IconFile)) info.PackageIcon = descriptor.IconFile!;
                if (!string.IsNullOrWhiteSpace(descriptor.PackageId)) info.PackageId = descriptor.PackageId;
            }

            try
            {
                var instance = (MarsPlugin)Activator.CreateInstance(type)!;
                logger.LogInformation("Plugin {PluginType} successfully instantiated. Methods overridden: Builder={HasBuilder}, App={HasApp}",
                    type.Name, hasConfigureBuilder, hasConfigureApp);

                result.Add(new LoadedPlugin(hasConfigureBuilder, hasConfigureApp, settings, instance, info));
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

            // Новый лейаут: дескриптор марс-plugin.json указывает входную сборку
            var descriptorFile = pluginRootFiles.FirstOrDefault(s => s.Name == PluginPackageDescriptor.FileName);
            if (descriptorFile is not null)
            {
                var descriptor = PluginDescriptorHelper.TryRead(descriptorFile.PhysicalPath!);
                if (descriptor is null)
                {
                    logger.LogWarning("Cannot parse descriptor {Descriptor} in {PluginDir}. Skipping.", descriptorFile.PhysicalPath, pluginDir.PhysicalPath);
                    continue;
                }

                var registryEntry = _registry.Get(descriptor.PackageId);
                if (registryEntry?.Disabled == true)
                {
                    logger.LogInformation("Plugin '{PackageId}' is disabled in registry — assembly not loaded.", descriptor.PackageId);
                    continue;
                }
                if (registryEntry?.PendingDelete == true)
                {
                    logger.LogInformation("Plugin '{PackageId}' is marked for deletion in registry — assembly not loaded.", descriptor.PackageId);
                    continue;
                }

                var pluginPhysicalDir = Path.GetDirectoryName(descriptorFile.PhysicalPath)!;
                var entryPhysical = Path.Combine(pluginPhysicalDir, descriptor.EntryAssembly);
                if (!File.Exists(entryPhysical))
                {
                    logger.LogWarning("Descriptor entry assembly '{Entry}' not found in {PluginDir}. Skipping.", descriptor.EntryAssembly, pluginDir.PhysicalPath);
                    continue;
                }

                logger.LogDebug("Found plugin by descriptor: {Entry}", entryPhysical);
                yield return new PluginConfig
                {
                    AssemblyPath = entryPhysical,
                    ContentRootPath = pluginPhysicalDir,
                };
                continue;
            }

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
