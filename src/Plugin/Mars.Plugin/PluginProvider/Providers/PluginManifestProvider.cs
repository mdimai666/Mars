using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Mars.Plugin.Dto;
using Mars.Plugin.Front.Abstractions;
using Mars.Plugin.PluginProvider.Dto;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace Mars.Plugin.PluginProvider.Providers;

public class PluginManifestProvider
{
    private readonly Assembly _assembly;

    public IReadOnlyCollection<EndpointJsonDto> Files { get; private set; }

    public PluginManifestProvider(Assembly assembly)
    {
        _assembly = assembly;
        var projectName = _assembly.GetName().Name;
        var targetDir = Path.GetDirectoryName(_assembly.Location)!;

        var manifestFileName = projectName + ".staticwebassets.endpoints.json";
        var manifestFilePath = Path.Combine(targetDir, manifestFileName);
        if (!File.Exists(manifestFilePath))
        {
            Files = [];
            return;
        }

        var pluginStaticwebassetsEndpoints = JsonSerializer.Deserialize<StaticwebassetsEndpointsManifestJson>(File.ReadAllText(manifestFilePath))!;
        var marsDevAdminEndpoints = MarsDevAdminEndpoints();

        Files = FilterFiles(marsDevAdminEndpoints, pluginStaticwebassetsEndpoints);
    }

    // Пример: .t9o416ijcu.wasm -> .wasm, .kyineex1gm.js -> .js
    // Ищет точку и от 8 до 16 символов хэша, после которых идет либо одно расширение (.wasm), либо два (.wasm.gz)
    private static readonly Regex FingerprintRegex = new(
        @"\.[a-z0-9]{8,16}(?=\.[a-zA-Z0-9]+(?:\.(gz|br))?$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static IReadOnlyCollection<EndpointJsonDto> FilterFiles(StaticwebassetsEndpointsManifestJson marsDevAdminEndpoints,
                                                                    StaticwebassetsEndpointsManifestJson pluginStaticwebassetsEndpoints)
    {
        static string NormalizePath(EndpointJsonDto endpoint)
        {
            var originalFile = endpoint.EndpointProperties
                .FirstOrDefault(p => p.Name == "original-file" || p.Name == "label")?.Value;

            var pathToClean = originalFile ?? endpoint.AssetFile;

            var gf = FingerprintRegex.Match(endpoint.AssetFile);

            return FingerprintRegex.Replace(pathToClean, "");
        }

        /*
         Предыдущие проблемы
        - original file name может у некоторых не быть
        - почему то сжатых файлов .br нет у marsDevAdminEndpoints
         */

        var marsOriginalFilesMap = marsDevAdminEndpoints.Endpoints
            .Select(NormalizePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var gzFiles = marsOriginalFilesMap.Where(s => s.EndsWith(".gz")).ToList();
        var brFiles = marsOriginalFilesMap.Where(s => s.EndsWith(".br")).ToList();
        if (brFiles.Count == 0)
        {
            marsOriginalFilesMap = marsOriginalFilesMap.Concat(gzFiles.Select(f => f[..^3] + ".br")).ToHashSet();
        }

        var pluginEndpoints = pluginStaticwebassetsEndpoints.Endpoints
            // 1. Отсекаем отладочный мусор
            .Where(f => !f.AssetFile.EndsWith(".pdb") && !f.AssetFile.EndsWith(".pdb.gz"))
            // 2. Исключаем специфичные файлы самого плагина (если они не нужны)
            .Where(s => !s.AssetFile.StartsWith("_framework/Mars.Plugin.Kit", StringComparison.OrdinalIgnoreCase)
                        // 3. Исключаем файлы ICU
                        && !s.AssetFile.StartsWith("_framework/icudt_")
                        && !s.AssetFile.StartsWith("_framework/Microsoft.DotNet.HotReload"))
            // 4. Исключаем все файлы, которые уже есть в основном приложении (сравнение идет без фингерпринтов!)
            //.Where(s => !marsOriginalFilesMap.Contains(NormalizePath(s)))
            .Where(s =>
            {
                //s.AssetFile.Contains("AppFront.Main") && s.AssetFile.EndsWith(".wasm")
                string v = NormalizePath(s);
                return !marsOriginalFilesMap.Contains(v);
            })
            .DistinctBy(s => s.AssetFile)
            .ToList();

        return pluginEndpoints;
    }

    private ProjectDependencies MarsDeps()
    {
        var assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var marsReleaseDepsJsonFile = Path.Combine(assemblyFolder, "Mars.deps.json");
        var marsWebAppDependencies = new ProjectDependencies(marsReleaseDepsJsonFile);
        return marsWebAppDependencies;
    }

    StaticwebassetsEndpointsManifestJson? MarsEndpoints()
    {
        var assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var manifestFileName = "Mars.staticwebassets.endpoints.json";
        var manifestFilePath = Path.Combine(assemblyFolder, manifestFileName);
        if (!File.Exists(manifestFilePath)) return null;
        var manifest = JsonSerializer.Deserialize<StaticwebassetsEndpointsManifestJson>(File.ReadAllText(manifestFilePath))!;
        return manifest;
    }

    StaticwebassetsEndpointsManifestJson MarsDevAdminEndpoints()
    {
        var assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var manifestFileName = "AppAdmin.staticwebassets.endpoints.json";
        var manifestFilePath = Path.Combine(assemblyFolder, manifestFileName);
        if (!File.Exists(manifestFilePath)) throw new FileNotFoundException($"{manifestFileName} not found. File is Required!");
        var manifest = JsonSerializer.Deserialize<StaticwebassetsEndpointsManifestJson>(File.ReadAllText(manifestFilePath))!;
        return manifest;
    }

    public MarsFrontPluginManifest GenerateManifest(WebApplication app, PluginData pluginData, ILogger logger)
    {
        var pluginUrl = $"/_plugin/{pluginData.Info.KeyName}";
        pluginData.Info.ManifestFile = $"{pluginUrl}/{MarsFrontPluginManifest.DefaultManifestFileName}";

        var simplifiedJson = Files.Select(s => new EndpointJsonDtoSimplified()
        {
            AssetFile = s.AssetFile,
            Route = s.Route,
        }).ToArray();

        var pluginInfo = new FrontPluginInfo
        {
            Version = pluginData.Info.Version,
            ProjectName = pluginData.Info.KeyName,
            StaticWebassets = new()
            {
                Endpoints = simplifiedJson
            }
        };

        var isDebug = true;

        var manifest = new MarsFrontPluginManifest()
        {
            IsDebug = isDebug,
        };
        manifest.Plugins[pluginData.Info.KeyName] = pluginInfo;

        return manifest;
    }
}
