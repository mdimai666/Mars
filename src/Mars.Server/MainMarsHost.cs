using System.Reflection;
using Mars.CommandLine.Abstractions;
using Mars.Contracts.Dto.Files;
using Mars.Options.Services;
using Mars.Server.Abstractions.Attributes;
using Mars.Server.Abstractions.Handlers;
using Mars.Server.Abstractions.Managers;
using Mars.Server.Abstractions.Services;
using Mars.Server.Abstractions.Validators;
using Mars.Server.CommandLine;
using Mars.Server.Contracts.Options;
using Mars.Server.Handlers;
using Mars.Server.Managers;
using Mars.Server.Seeding;
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
        services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());

        services.AddSingleton<IActionManager, XActionManager>();
        services.AddSingleton<IEventManager, EventManager>();

        services.AddSingleton<IActionHistoryService, ActionHistoryService>();
        services.AddSingleton<LogMaintenanceStartupService>();
        services.AddSingleton<IMarsSystemService, MarsSystemService>();
        //services.AddSingleton<ModelInfoService>(); // Mars\Mars.Contracts\Tools\ModelInfoService.cs

        services.AddScoped<IInitialSiteDataViewModelHandler, InitialSiteDataViewModelHandler>();

        services.AddScoped<IValidatorFactory, ValidatorFactory>();

        services.AddSingleton<ISeedFirstOptionHandler, SeedFirstOptionHandler>();

        // Потребители: FunctionNodeImpl (Nodes) и KernelFactory_v2 (SemanticKernel.Host)
        services.AddSingleton<IServiceCollection>(services);

        UseFileStorages(services, wenv);

        //read (may object viewer)
        // Microsoft.AspNetCore.Identity.IEmailSender

        RegisterAIToolScenarioProviders(services);

        return services;
    }

    public static IApplicationBuilder UseMarsHost(this WebApplication app, IServiceCollection serviceCollection)
    {
        // MigrationCommandCli не регистрируется здесь: «migrate» — базовая команда,
        // исполняется до ConfigureApp и берётся из initialCommands в MarsWebAppStartup.
        return app;
    }

    public static IServiceProvider UseMarsServerOptions(this IServiceProvider services)
    {
        var optionService = services.GetRequiredService<IOptionService>();
        optionService.RegisterOption<SiteSettings>();
        optionService.RegisterOption<ApiOption>();
        optionService.RegisterOption<MaintenanceModeOption>();
        optionService.GetOption<SiteSettings>();
        return services;
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
