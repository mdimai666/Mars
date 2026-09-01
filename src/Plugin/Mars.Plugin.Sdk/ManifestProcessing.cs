using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using Mars.Plugin.Front.Abstractions;
using Mars.Plugin.Sdk.Dto;
using Mars.Plugin.Sdk.Models;

namespace Mars.Plugin.Sdk;

internal class ManifestProcessing
{
    public static void ProcessPublishManifest(ProcessScriptSettings settings, ProjectDependencies marsDependencies, ProjectDependencies projectDependencies, List<FileInfo> otherFiles, DirectoryInfo publish, DirectoryInfo wwwroot, HashSet<string> marsDlls)
    {
        //var manifestFileName0 = settings.ProjectName + ".staticwebassets.endpoints.json";
        //var manifestFilePath0 = Path.Combine(publish.FullName, manifestFileName0);
        //var manifest0 = JsonSerializer.Deserialize<StaticwebassetsEndpointsManifestJson>(File.ReadAllText(manifestFilePath0));

#if OLD
        var requireFiles = new List<EndpointJsonDto>();

        var otherFilesWwwRootRelHash = otherFiles.Where(s => s.FullName.StartsWith(wwwroot.FullName))
                                                    .Select(s => Path.GetRelativePath(wwwroot.FullName, s.FullName).Replace("\\", "/")).ToHashSet();
        foreach (var endpoint in manifest.Endpoints)
        {
            if (otherFilesWwwRootRelHash.Contains(endpoint.AssetFile))
                requireFiles.Add(endpoint);

        }

        var simplifiedJson = requireFiles.Select(s => new EndpointJsonDtoSimplified { AssetFile = s.AssetFile, Route = s.Route }).ToArray();
#endif

        var frontAssemblies = ReadFrontPluginAssemblies(settings, projectDependencies);
        var pluginManifest = new MarsFrontPluginManifest() { IsDebug = true };

        foreach (var assembly in frontAssemblies)
        {
            var projectName = assembly.GetName().Name!;
            //var targetDir = new DirectoryInfo(Path.Combine(settings.ProjectDir, "..", projectName, settings.OutDir));
            var manifestFileName = projectName + ".staticwebassets.endpoints.json";
            //var manifestFilePath = Path.Combine(targetDir.FullName, manifestFileName);
            var manifestFilePath = Path.Combine(publish.FullName, "..", manifestFileName);
            Console.WriteLine("+manifestFilePath= " + manifestFilePath);
            var manifest = JsonSerializer.Deserialize<StaticwebassetsEndpointsManifestJson>(File.ReadAllText(manifestFilePath));

            //HERE
            var simplifiedJson = FilterEndpoints(manifest.Endpoints, marsDependencies, projectDependencies, marsDlls).Select(s => new EndpointJsonDtoSimplified
            {
                AssetFile = s.AssetFile,
                Route = s.Route
            }).ToArray();
            //END

            string version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";

            var pluginInfo = new FrontPluginInfo
            {
                Version = version,
                ProjectName = projectName,
                StaticWebassets = new()
                {
                    Endpoints = simplifiedJson
                }
            };

            pluginManifest.Plugins[projectName] = pluginInfo;
        }

        WritePluginManifest(settings, pluginManifest);
    }

    static void WritePluginManifest(ProcessScriptSettings settings, MarsFrontPluginManifest manifest)
    {
        var pluginManifestWwwRoot = new DirectoryInfo(Path.Combine(settings.ProjectDir, settings.OutDir, "wwwroot"));
        if (!pluginManifestWwwRoot.Exists) pluginManifestWwwRoot.Create();
        var pluginManifestFilePath = Path.Combine(pluginManifestWwwRoot.FullName, MarsFrontPluginManifest.DefaultManifestFileName);

        var manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = manifest.IsDebug });
        File.WriteAllText(pluginManifestFilePath, manifestJson);

        Console.WriteLine($"Write plugin manifest: {pluginManifestFilePath}");
    }

    public static void ProcessDebugManifest(ProcessScriptSettings settings, ProjectDependencies marsDependencies, ProjectDependencies projectDependencies, HashSet<string> marsDlls)
    {
        //marsDlls - некоторые компоненты имеют под dll типа
        // Microsoft.FluentUI.AspNetCore.Components.Icons => _framework/Microsoft.FluentUI.AspNetCore.Components.Icons.Light.ziq8031mzs.wasm

        var frontAssemblies = ReadFrontPluginAssemblies(settings, projectDependencies);
        var pluginManifest = new MarsFrontPluginManifest() { IsDebug = true };

        foreach (var assembly in frontAssemblies)
        {
            var projectName = assembly.GetName().Name!;
            var targetDir = new DirectoryInfo(Path.Combine(settings.ProjectDir, "..", projectName, settings.OutDir));
            var manifestFileName = projectName + ".staticwebassets.endpoints.json";
            var manifestFilePath = Path.Combine(targetDir.FullName, manifestFileName);
            var manifest = JsonSerializer.Deserialize<StaticwebassetsEndpointsManifestJson>(File.ReadAllText(manifestFilePath));

            //var wwwroot = new DirectoryInfo(Path.Combine(settings.ProjectDir, settings.OutDir, "wwwroot"));
            //var targetDir_wwwroot = new DirectoryInfo(Path.Combine(targetDir.FullName, "wwwroot"));

            var simplifiedJson = FilterEndpoints(manifest.Endpoints, marsDependencies, projectDependencies, marsDlls).Select(s => new EndpointJsonDtoSimplified
            {
                AssetFile = s.AssetFile,
                Route = s.Route
            }).ToArray();

            string version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";

            var pluginInfo = new FrontPluginInfo
            {
                Version = version,
                ProjectName = projectName,
                StaticWebassets = new()
                {
                    Endpoints = simplifiedJson
                }
            };

            pluginManifest.Plugins[projectName] = pluginInfo;
        }

        WritePluginManifest(settings, pluginManifest);
    }

    static IEnumerable<Assembly> ReadFrontPluginAssemblies(ProcessScriptSettings settings, ProjectDependencies projectDependencies)
    {
        var rootDepends = projectDependencies.Packages[settings.ProjectName].Dependencies.Values
                                            .Select(s => projectDependencies.Libraries[s.Name])
                                            .Where(s => s.Type == LibraryType.Project && s.Name != settings.CurrentScriptProjectNugetName)
                                            .ToList();

        var loadContext = new AssemblyLoadContext("TempContext", true);
        loadContext.Resolving += (AssemblyLoadContext context, AssemblyName assemblyName) =>
        {
            // Пытаемся найти зависимую сборку в той же папке, что и основная
            string assemblyPath = Path.Combine(settings.ProjectDir, settings.OutDir, $"{assemblyName.Name}.dll");

            if (File.Exists(assemblyPath))
            {
                using (var fs = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read))
                {
                    return context.LoadFromStream(fs);
                }
            }
            return null; // Если сборка не найдена, будет использовано стандартное разрешение
        };

        foreach (var dependency in rootDepends)
        {
            var runtimeAssemlyName = projectDependencies.Packages[dependency.Name].Runtime.ElementAt(0);
            var assemblyFileName = runtimeAssemlyName.Key;

            var assemblyPath = Path.Combine(settings.ProjectDir, settings.OutDir, assemblyFileName);
            if (!File.Exists(assemblyPath)) continue;

            Assembly assembly;
            using (var fs = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read))
                assembly = loadContext.LoadFromStream(fs);

            Type[] assemblyTypes;
            try
            {
                assemblyTypes = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // часть типов ссылается на сборки Марса, которых нет в папке плагина
                // (они резолвятся хостом в рантайме) — для поиска стартапов достаточно
                // загружаемого подмножества
                assemblyTypes = ex.Types.Where(t => t is not null).ToArray()!;
            }

            var startups = assemblyTypes.Where(s => typeof(IWebAssemblyPluginFront).IsAssignableFrom(s));

            if (startups.Any())
            {
                yield return assembly;
            }
        }
    }

    static List<EndpointJsonDto> FilterEndpoints(List<EndpointJsonDto> endpoints, ProjectDependencies marsDependencies, ProjectDependencies projectDependencies, HashSet<string> marsDlls)
    {
        Func<string, string> extractFileName = (string endpoint) =>
        {
            var ext = Path.GetExtension(endpoint);
            if (ext == ".gz") return Path.GetFileName(Path.GetFileNameWithoutExtension(endpoint));
            if (ext == ".br") return Path.GetFileName(Path.GetFileNameWithoutExtension(endpoint));
            else return Path.GetFileName(endpoint);
        };

        var list = endpoints.Where(e =>
        {
            if (e.AssetFile.StartsWith("_content"))
            {
                var extractedName = e.AssetFile.Split('/', 3)[1];

                return !marsDependencies.Packages.ContainsKey(extractedName);
            }
            else if (e.AssetFile.StartsWith("_framework"))
            {
                var props = e.EndpointProperties.ToDictionary(s => s.Name, s => s.Value);
                /* props:
                "[fingerprint, ay8p6mgwfm]"
                "[integrity, sha256-biiaW6VwbTj5drYOl5nmJ60gRVK7u+814lRjsgvU5XM=]"
                "[label, _framework/Mars.Admin.Framework.wasm]"
                 */

                string extractedName; //like Mars.Admin.Framework
                string extractedFilename; //like Mars.Admin.Framework.wasm

                if (props.ContainsKey("label"))
                    extractedFilename = extractFileName(props["label"]);
                else
                    extractedFilename = extractFileName(e.Route);
                extractedName = Path.GetFileNameWithoutExtension(extractedFilename);

                if (ScriptFilesProcessing.IsFrameworkSystemFile(extractedFilename)) return false;
                if (marsDlls.Contains(extractedName + ".dll")) return false;

                if (extractedName.EndsWith(".resources"))
                {
                    // "_framework/ru/Mars.Contracts.resources.9r8ny6eq1l.wasm"
                    var prefix = e.AssetFile.Substring("_framework/".Length).Split(extractedName, 2)[0];
                    var resName = prefix + extractedName + ".dll";
                    if (marsDlls.Contains(resName))
                    {
                        return false;
                    }
                }

                //if (marsDependencies.Packages.ContainsKey(extractedName + ".resources")) return false;

                return !marsDependencies.Packages.ContainsKey(extractedName);
            }

            return true;
        }).ToList();

        return list;
    }
}
