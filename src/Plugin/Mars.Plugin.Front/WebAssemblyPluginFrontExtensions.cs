using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.Loader;
using AppFront.Main.OptionEditForms;
using Mars.Core.Extensions;
using Mars.Nodes.Core;
using Mars.Plugin.Front.Abstractions;
using Mars.Shared.Contracts.Plugins;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Mars.Plugin.Front;

public static class WebAssemblyPluginFrontExtensions
{
    private static List<IWebAssemblyPluginFront> pluginStartups = [];

    public static List<Assembly> PluginLoadAssemblies = [];

    public static async Task AddRemotePluginAssemblies(this WebAssemblyHostBuilder builder, string backendUrl)
    {
        var http = new HttpClient() { BaseAddress = new Uri(backendUrl) };

        var loadAssemblies = new List<Assembly>();

        await LoadManifest(loadAssemblies, http);

        foreach (var assembly in loadAssemblies)
        {
            var assemblyTypes = assembly.GetTypes();
            //Console.WriteLine("types count: " + assemblyTypes.Count());
            var startups = assemblyTypes.Where(s => typeof(IWebAssemblyPluginFront).IsAssignableFrom(s));
            //Console.WriteLine("startups count: " + startups.Count());

            foreach (var startupType in startups)
            {
                var instance = (IWebAssemblyPluginFront)Activator.CreateInstance(startupType)!;
                instance.ConfigureServices(builder);
                pluginStartups.Add(instance);
            }
        }

        PluginLoadAssemblies = loadAssemblies;

        NodesLocator.RefreshDict();
        NodeFormsLocator.RefreshDict();
        OptionsFormsLocator.RefreshDict();
    }

    public static void UseRemotePluginAssemblies(this WebAssemblyHost app)
    {
        foreach (var startupInstance in pluginStartups)
        {
            startupInstance.ConfigureApplication(app);
        }
    }

    private static async Task LoadManifest(List<Assembly> loadAssemblies, HttpClient http)
    {
        var runtimeManifests = (await http.GetFromJsonAsync<PluginManifestInfoResponse[]>("/api/Plugin/RuntimePluginManifests"))!;

        Console.WriteLine($"LoadPluginRemoteAssemblies: [{string.Join(',', runtimeManifests.Select(s => s.Name))}]");

        // манифест файлы
        foreach (var manifestInfo in runtimeManifests)
        {
            //var pluginEndpointsFile = $"/_plugin/Mars.TelegramPlugin/{MarsFrontPluginManifest.DefaultManifestFileName}";
            var pluginEndpointsFile = manifestInfo.Uri;
            var manifest = await http.GetFromJsonAsync<MarsFrontPluginManifest>(pluginEndpointsFile);

            // каждый плагин объявляет какие компоненты загружать
            foreach (var pluginDefinition in manifest.Plugins.Values)
            {
                //var enpoints = manifest.Plugins["Mars.TelegramPlugin.Nodes"].StaticWebassets.Endpoints;
                var enpoints = pluginDefinition.StaticWebassets.Endpoints;
                var pluginDlls = enpoints!.Where(s => s.AssetFile.EndsWith(".wasm")).DistinctBy(s => s.AssetFile).OrderByDescending(s => s.AssetFile.Length).ToList();

                //var pluginDllFullUrls = pluginDlls.Select(s => $"/_plugin/Mars.TelegramPlugin/{s.AssetFile}").ToList();

                foreach (var pluginDll in pluginDlls)
                {
                    var pluginDllFull = $"/_plugin/{manifestInfo.Name}/" + pluginDll.AssetFile;

                    try
                    {
                        Console.WriteLine($"start load: {pluginDll.AssetFile}");

                        var dllBytes = await http.GetByteArrayAsync(pluginDllFull);
                        Console.WriteLine($"Bytes load: {dllBytes.Length.ToHumanizedSize()}");
                        var assembly = Assembly.Load(dllBytes);

                        loadAssemblies.Add(assembly);
                        using Stream stream = new MemoryStream(dllBytes);
                        AssemblyLoadContext.Default.LoadFromStream(stream);

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"({pluginDll.AssetFile}) Ошибка загрузки: {ex.Message}");
                    }
                }
            }
        }
    }
}
