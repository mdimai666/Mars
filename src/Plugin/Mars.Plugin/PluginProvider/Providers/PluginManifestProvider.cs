using System.Reflection;
using System.Text.Json;
using Mars.Plugin.Dto;
using Mars.Plugin.Front.Abstractions;
using Mars.Plugin.PluginProvider.Dto;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

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

        var manifest = JsonSerializer.Deserialize<StaticwebassetsEndpointsManifestJson>(File.ReadAllText(manifestFilePath));

        //var mars_deps = MarsDeps();

        var marsEndpoints = MarsDevAdminEndpoints();

        var marsEndpointsMap = marsEndpoints.Endpoints.Select(s => s.AssetFile).ToHashSet();

        //var without_fingerprint

        var pluginEndpoints = manifest.Endpoints.Where(s => !marsEndpointsMap.Contains(s.AssetFile))
                                                .Where(s => !s.AssetFile.StartsWith("_framework/Mars.Plugin.Kit"))
                                                .Where(s => !s.AssetFile.StartsWith("_framework/icudt_"))
                                                .Where(s => !s.AssetFile.StartsWith("_framework/System.")) //TODO: если fingerprint отличается, то начинает поподать мусор
                                                .Where(f => !f.AssetFile.EndsWith(".pdb") && !f.AssetFile.EndsWith(".pdb.gz"))
                                                //.Where(f => !f.AssetFile.EndsWith(".modules.json"))
                                                .DistinctBy(s => s.AssetFile)
                                                .ToList();

        Files = pluginEndpoints;
    }

    private ProjectDependencies MarsDeps()
    {
        var assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var marsReleaseDepsJsonFile = Path.Combine(assemblyFolder, "Mars.deps.json");
        var marsWebAppDependencies = new ProjectDependencies(marsReleaseDepsJsonFile);
        return marsWebAppDependencies;
    }

    StaticwebassetsEndpointsManifestJson MarsEndpoints()
    {
        var assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var manifestFileName = "Mars.staticwebassets.endpoints.json";
        var manifestFilePath = Path.Combine(assemblyFolder, manifestFileName);
        var manifest = JsonSerializer.Deserialize<StaticwebassetsEndpointsManifestJson>(File.ReadAllText(manifestFilePath))!;
        return manifest;
    }

    StaticwebassetsEndpointsManifestJson MarsDevAdminEndpoints()
    {
        var assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var manifestFileName = "AppAdmin.staticwebassets.endpoints.json";
        var manifestFilePath = Path.Combine(assemblyFolder, manifestFileName);
        var manifest = JsonSerializer.Deserialize<StaticwebassetsEndpointsManifestJson>(File.ReadAllText(manifestFilePath))!;
        return manifest;
    }

    public void ProvideManifest(WebApplication app, PluginData pluginData)
    {
        if (Files.Count == 0) return;

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

        //var manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = isDebug });
        //File.WriteAllText(manifest, manifestJson);

        var pluginManifestUrl = $"/_plugin/{pluginData.Info.KeyName}/{MarsFrontPluginManifest.DefaultManifestFileName}";
        app.MapGet(pluginManifestUrl, () =>
        {
            return Results.Json(manifest);
            //await context.Response.WriteAsJsonAsync(manifestJson);
        });
    }
}
