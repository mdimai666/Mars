using System.Reflection;
using Mars.Contracts.Dto.Files;
using Mars.Options.Abstractions.Services;
using Mars.Server.Abstractions.Attributes;
using Mars.Server.Abstractions.Handlers;
using Mars.Server.Abstractions.Managers;
using Mars.Server.Abstractions.Services;
using Mars.Server.Abstractions.Startup;
using Mars.Server.Abstractions.Validators;
using Mars.Server.Contracts.Options;
using Mars.Server.Handlers;
using Mars.Server.Managers;
using Mars.Server.Seeding;
using Mars.Server.Services;
using Mars.Server.XActions;
using Mars.XActions.Abstractions.Managers;
using Mars.XActions.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MOptions = Microsoft.Extensions.Options.Options;

namespace Mars.Server;

public static class MainServer
{
    public static IServiceCollection AddMarsServer(this IServiceCollection services, IWebHostEnvironment wenv)
    {
        services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());

        //Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore
        //services.AddDatabaseDeveloperPageExceptionFilter();

        services.AddSingleton<IEventManager, EventManager>();

        services.AddSingleton<IActionHistoryService, ActionHistoryService>();
        services.AddSingleton<LogMaintenanceStartupService>();
        services.AddSingleton<IMarsSystemService, MarsSystemService>();
        //services.AddSingleton<ModelInfoService>(); // Mars\Mars.Contracts\Tools\ModelInfoService.cs

        services.AddScoped<IInitialSiteDataViewModelHandler, InitialSiteDataViewModelHandler>();

        services.AddScoped<IValidatorFactory, ValidatorFactory>();

        services.AddSingleton<ISeedFirstOptionHandler, SeedFirstOptionHandler>();

        services.AddXActionHandlers(typeof(ClearCacheAct).Assembly);

        // Потребители: FunctionNodeImpl (Nodes) и KernelFactory_v2 (SemanticKernel.Host)
        services.AddSingleton<IServiceCollection>(services);

        UseFileStorages(services, wenv);

        //read (may object viewer)
        // Microsoft.AspNetCore.Identity.IEmailSender

        RegisterAIToolScenarioProviders(services);

        return services;
    }

    public static IApplicationBuilder UseMarsServer(this WebApplication app)
    {
        // MigrationCommandCli не регистрируется здесь: «migrate» — базовая команда,
        // исполняется до ConfigureApp и берётся из initialCommands в MarsWebAppStartup.
        app.RegisterHostXActions();
        return app;
    }

    /// <summary>
    /// Регистрация кор-опций. Вызывается в бутстрапе до сидов
    /// (MigrationCommandCli дублирует порядок: миграции → опции → сид).
    /// </summary>
    public static IServiceProvider UseMarsServerOptions(this IServiceProvider services)
    {
        var optionService = services.GetRequiredService<IOptionService>();
        optionService.RegisterOption<SiteSettings>();
        optionService.RegisterOption<ApiOption>();
        optionService.RegisterOption<MaintenanceModeOption>();
        optionService.GetOption<SiteSettings>();

        if (optionService.IsDevelopment)
        {
            var startupInfo = services.GetRequiredService<IMarsStartupInfo>();
            optionService.SetConstOption(new ServerWorkDirectoryOption
            {
                WorkDirectory = startupInfo.StartWorkDirectory,
            }, appendToInitialSiteData: true);
        }

        return services;
    }

    /// <summary>
    /// Хостовые XActions: кеш, отладочные команды. Регистрируются без контекстов
    /// админки — контексты навешивает оверлеем сторона, знающая админку (Mars.Admin.Host).
    /// </summary>
    static IApplicationBuilder RegisterHostXActions(this WebApplication app)
    {
        var actionManager = app.Services.GetRequiredService<IActionManager>();

        actionManager.Add(a =>
        {
            a.Id(ClearCacheAct.CommandId)
             .Label("Очистить кеш")
             .Category("Хост")
             .Recommended(10)
             .Handler<ClearCacheAct>();
        });

        actionManager.Add(a => a
            .Id("App.Logs")
            .Label("App logs")
            .Category("Разработка")
            .Link("/dev/builder/debug"));

#if DEBUG
        actionManager.Add(a =>
        {
            a.Id(DummyAct.CommandId)
             .Label("DummyAct")
             .Category("Отладка")
             .System()
             .Handler<DummyAct>();
        });

        actionManager.Add(a => a
            .Id(FormTestAct.CommandId)
            .Label("Тест формы XAction")
            .Description("Строка, число, bool и выбор из списка — тост покажет введённое")
            .Category("Отладка")
            .Argument(FormTestAct.TextArg, "Строка", required: true)
            .Argument(FormTestAct.NumberArg, "Число", XActionArgumentType.Number, defaultValue: "42")
            .Argument(FormTestAct.BoolArg, "Флаг", XActionArgumentType.Bool, defaultValue: "true")
            .Argument(FormTestAct.ChoiceArg, "Выбор из списка", XActionArgumentType.Choice, options:
            [
                new() { Key = "one", Label = "Первый" },
                new() { Key = "two", Label = "Второй" },
                new() { Key = "three", Label = "Третий" },
            ])
            .Handler<FormTestAct>());

        actionManager.Add(a => a
            .Id(FrontDemoXAction.CommandId)
            .Label(FrontDemoXAction.Label)
            .Description("Исполняется на клиенте, хост такую команду не выполняет")
            .Category("Отладка")
            .FrontAction());
#endif

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
