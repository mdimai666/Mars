using System.Reflection;
using Mars.Contracts.Dto.Files;
using Mars.Options.Services;
using Mars.Server.Abstractions.Attributes;
using Mars.Server.Abstractions.Managers;
using Mars.Server.Abstractions.Services;
using Mars.Server.Abstractions.Validators;
using Mars.Server.Handlers;
using Mars.Server.Managers;
using Mars.Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MOptions = Microsoft.Extensions.Options.Options;

namespace Mars.Server;

public static class MainMarsHost
{
    public static IServiceCollection AddMarsHost(this IServiceCollection services, IWebHostEnvironment wenv)
    {
        services.AddSingleton<IActionManager, XActionManager>();
        services.AddSingleton<IEventManager, EventManager>();

        services.AddSingleton<IActionHistoryService, ActionHistoryService>();
        //services.AddSingleton<ModelInfoService>(); // Mars\Mars.Contracts\Tools\ModelInfoService.cs

        services.AddScoped<InitialSiteDataViewModelHandler>();

        services.AddScoped<IValidatorFactory, ValidatorFactory>();

        UseFileStorages(services, wenv);

        //read (may object viewer)
        // Microsoft.AspNetCore.Identity.IEmailSender

        RegisterAIToolScenarioProviders(services);

        return services;
    }

    public static IApplicationBuilder UseMarsHost(this WebApplication app, IServiceCollection serviceCollection)
    {
        return app;
    }

    static void UseFileStorages(IServiceCollection services, IWebHostEnvironment wenv)
    {
        services.AddSingleton<IFileStorage, FileStorage>();
        services.AddSingleton<IOptions<FileHostingInfo>>(sp => MOptions.Create(sp.GetRequiredService<IOptionService>().FileHostingInfo()));

        var dataDirHostingInfo = MOptions.Create(new FileHostingInfo()
        {
            Backend = null,
            PhysicalPath = new Uri(Path.Combine(wenv.ContentRootPath, "data"), UriKind.Absolute),
            RequestPath = ""
        });

        var dataFs = new FileStorage(dataDirHostingInfo);

        services.AddKeyedSingleton<IOptions<FileHostingInfo>>("data", dataDirHostingInfo);
        services.AddKeyedSingleton<IFileStorage>("data", dataFs);
    }

    static IServiceCollection RegisterAIToolScenarioProviders(this IServiceCollection services)
    {
        services.AddSingleton<IAIToolScenarioProvidersLocator, AIToolScenarioProvidersLocator>();//Надо изменить логику хранений сценариев.
        //var toolMap = new Dictionary<string, Type>();

        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location));

        foreach (var type in assemblies.SelectMany(a => a.GetTypes()))
        {
            if (!type.IsClass || type.IsAbstract)
                continue;

            var attr = type.GetCustomAttribute<RegisterAIToolAttribute>();
            if (attr == null)
                continue;

            if (typeof(IAIToolScenarioProvider).IsAssignableFrom(type))
            {
                var key = attr.Key ?? type.Name;
                services.AddKeyedTransient(typeof(IAIToolScenarioProvider), key, type);
            }

            //toolMap[attr.Key] = type;
        }

        return services;
    }
}
