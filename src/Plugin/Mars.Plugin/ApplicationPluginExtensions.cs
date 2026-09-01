using System.Reflection;
using Mars.Contracts.Dto.Files;
using Mars.Options.Abstractions.Services;
using Mars.Plugin.Abstractions.Services;
using Mars.Plugin.Contracts.Options;
using Mars.Plugin.Dto;
using Mars.Plugin.Services;
using Mars.Server.Abstractions.Services;
using Mars.Storage.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MOptions = Microsoft.Extensions.Options.Options;

namespace Mars.Plugin;

public static class ApplicationPluginExtensions
{
    private static readonly bool isTesting = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Test", StringComparison.OrdinalIgnoreCase) ?? false;

    public static WebApplicationBuilder AddPlugins(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());

        // Плагины регистрируют сервисы и MVC-части строго до Build(), поэтому
        // PluginManager создаётся сразу, без сборки провайдера.
        var dataFileStorage = builder.Services.GetOrCreateDataFileStorage(builder.Environment);
        // без using: логгер живёт вместе с PluginManager весь срок работы приложения
        var loggerFactory = LoggerFactory.Create(logBuilder => logBuilder.AddConsole());

        var pluginManager = new PluginManager(loggerFactory.CreateLogger<PluginManager>(), dataFileStorage);
        pluginManager.ConfigureBuilder(builder);
        builder.Services.AddSingleton(pluginManager);
        builder.Services.AddControllers().AddPluginsAsPartOfMvc(pluginManager.Plugins);

        builder.Services.AddSingleton<IPluginService, PluginService>();
        return builder;
    }

    /// <summary>
    /// Keyed-регистрация "data" появляется в MainServer; для хостов без него
    /// (автономные тесты) регистрируем здесь тем же экземпляром.
    /// </summary>
    static IFileStorage GetOrCreateDataFileStorage(this IServiceCollection services, IWebHostEnvironment environment)
    {
        var registered = services.FirstOrDefault(s => s.ServiceType == typeof(IFileStorage) && s.ServiceKey as string == "data");
        if (registered?.ImplementationInstance is IFileStorage instance) return instance;

        var dataDirHostingInfo = MOptions.Create(new FileHostingInfo()
        {
            Backend = null,
            PhysicalPath = new Uri(Path.Combine(environment.ContentRootPath, "data"), UriKind.Absolute),
            RequestPath = ""
        });
        var dataFs = new FileStorage(dataDirHostingInfo);
        services.AddKeyedSingleton<IFileStorage>("data", dataFs);
        return dataFs;
    }

    public static void ApplyPluginMigrations(this WebApplication app)
    {
        if (isTesting) return;

        var pluginManager = app.Services.GetRequiredService<PluginManager>();
        pluginManager.ApplyPluginMigrations(app.Services, app.Configuration);
    }

    public static void UsePlugins(this WebApplication app)
    {
        app.Services.GetRequiredService<IOptionService>().RegisterOption<PluginManagerSettingsOption>();

        var pluginManager = app.Services.GetRequiredService<PluginManager>();
        pluginManager.UsePlugins(app);
    }

    static IMvcBuilder AddPluginsAsPartOfMvc(this IMvcBuilder mvcBuilder, IEnumerable<LoadedPlugin> plugins)
    {
        foreach (var p in plugins)
        {
            var assembly = p.Plugin.GetType().Assembly;
            mvcBuilder.PartManager.ApplicationParts.Add(new AssemblyPart(assembly));
        }

        return mvcBuilder;
    }

}
