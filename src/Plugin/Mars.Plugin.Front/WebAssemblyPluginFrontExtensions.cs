using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.Loader;
using Mars.Core.Extensions;
using Mars.Nodes.Core;
using Mars.Plugin.Front.Abstractions;
using Mars.Shared.Contracts.Plugins;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mars.Plugin.Front;

public static class WebAssemblyPluginFrontExtensions
{
    public static async Task LoadPluginRemoteAssemblies(this WebAssemblyHost app, WebAssemblyHostBuilder builder)
    {
        // https://learn.microsoft.com/ru-ru/aspnet/core/blazor/webassembly-lazy-load-assemblies?view=aspnetcore-9.0
        LazyAssemblyLoader LazyLoader = app.Services.GetRequiredService<LazyAssemblyLoader>();
        var baseHttp = app.Services.GetRequiredService<HttpClient>();
        var http = new HttpClient() { BaseAddress = baseHttp.BaseAddress };
        var logger = app.Services.GetRequiredService<ILogger<IWebAssemblyPluginFront>>();

        var loadAssemblies = new List<Assembly>();

        await LoadManifest(loadAssemblies, logger, http);

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
                instance.ConfigureApplication(app);
            }

            //NodesLocator.RegisterAssembly(assembly);
            //NodeFormsLocator.RegisterAssembly(assembly);
        }

        NodesLocator.RefreshDict();
        NodeFormsLocator.RefreshDict();
    }

    private static async Task LoadManifest(List<Assembly> loadAssemblies, ILogger logger, HttpClient http)
    {

        var runtimeManifests = (await http.GetFromJsonAsync<PluginManifestInfoResponse[]>("/api/Plugin/RuntimePluginManifests"))!;

        Console.WriteLine($"LoadPluginRemoteAssemblies: [{string.Join(',', runtimeManifests.Select(s => s.Name))}]" );

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
                        logger.LogInformation($"start load: {pluginDll.AssetFile}");

                        var dllBytes = await http.GetByteArrayAsync(pluginDllFull);
                        Console.WriteLine($"Bytes load: {dllBytes.Length.ToHumanizedSize()}");
                        var assembly = Assembly.Load(dllBytes);

                        loadAssemblies.Add(assembly);
                        using Stream stream = new MemoryStream(dllBytes);
                        AssemblyLoadContext.Default.LoadFromStream(stream);

                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"({pluginDll.AssetFile}) Ошибка загрузки: {ex.Message}");
                    }
                }
            }
        }
    }
}
