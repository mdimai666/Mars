using System.Diagnostics;
using System.Reflection;
using Mars.Core.Models;
using Mars.Options.Abstractions.Services;
using Mars.Server.Abstractions.Services;
using Mars.SiteEngine.Abstractions.Constants.Website;
using Mars.SiteEngine.Abstractions.Models;
using Mars.SiteEngine.Abstractions.Services;
using Mars.SiteEngine.Abstractions.WebSite;
using Mars.SiteEngine.Abstractions.WebSite.Scripts;
using Mars.SiteEngine.Contracts.Options;
using Mars.SiteEngine.Host.Endpoints;
using Mars.SiteEngine.Host.Handlers;
using Mars.SiteEngine.Host.Services;
using Mars.SiteEngine.Host.Templators;
using Mars.SiteEngine.Host.WebSite.Scripts;
using Mars.TemplateEngine.Host;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.SiteEngine.Host;

public static class MainSiteEngine
{
    public static IServiceCollection AddMarsSiteEngine(this IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());

        services.AddSingleton<IWebSiteProcessor, MapWebSiteProcessor>();
        services.AddSingleton<IWebRenderEngineLocator, WebRenderEngineLocator>();
        services.AddSingleton<ITemplatorFeaturesLocator, TemplatorFeaturesLocator>();
        services.AddSingleton<IFrontManager, FrontManager>();
        services.AddSingleton<FrontTemplateService>();
        services.AddSingleton<IFrontFilesService, FrontFilesService>();
        services.AddSingleton<FrontRenderWarmupService>();
        services.AddScoped<IPageRenderService, PageRenderService>();

        services.AddScoped<IFaviconGeneratorHandler, FaviconGeneratorHandler>();
        services.AddScoped<AdminFrontRenderHandler>();
        services.AddScoped<SiteFaviconConfiguratorHandler>();

        AddSiteScriptsBuilders(services);

        // подсистема TemplateEngine потребляется SiteEngine'ом как внешняя
        services.AddMarsTemplateEngines();

        return services;
    }

    /// <summary>
    /// Стартап-подготовка: опции фронтов, фронт админки, скрипт-билдеры,
    /// шаблонизатор-функции. Вызывается в зоне модулей после сидов;
    /// фронтовые middleware — отдельно в <see cref="UseMarsSiteEngine"/>.
    /// Прогрев рендера — <see cref="FrontRenderWarmupService"/> (IMarsAppLifetimeService).
    /// </summary>
    public static IServiceProvider UseMarsSiteEngineStartup(this IServiceProvider services)
    {
        UseMarsSiteEngineOptions(services);

        // специальный фронт админки (data/admin/front) — создаётся один раз при старте
        services.GetRequiredService<FrontTemplateService>().EnsureAdminFront();

        UseSiteScriptsBuilders(services);

        RegisterTemplatorFunctions(services);

        return services;
    }

    /// <summary>
    /// Фронтовые middleware. Должны быть последними в пайплайне:
    /// фолбэк рендерит фронт, только если запрос никто не обработал.
    /// </summary>
    public static IApplicationBuilder UseMarsSiteEngine(this WebApplication app)
    {
        UseSiteEngineMiddlewares(app);

        return app;
    }

    static IServiceProvider UseMarsSiteEngineOptions(this IServiceProvider services)
    {
        var optionService = services.GetRequiredService<IOptionService>();
        optionService.RegisterOption<FrontsOption>();
        optionService.RegisterOption<SEOOption>();
        optionService.GetOption<SEOOption>();
        optionService.RegisterOption<FaviconOption>(opt => _ = OnChangeFaviconOption(opt, services));
        optionService.RegisterOption<FaviconOptionGenaratedValues>();

        var configuration = services.GetRequiredService<IConfiguration>();
        services.MigrateAppFrontToOption(configuration);
        services.EnsureDefaultFront(configuration);
        return services;
    }

    static void RegisterTemplatorFunctions(IServiceProvider services)
    {
        ITemplatorFeaturesLocator tflocator = services.GetRequiredService<ITemplatorFeaturesLocator>();

        var functions = tflocator.Functions;

        functions.Add(nameof(TemplatorRegisterFunctions.Paginator), TemplatorRegisterFunctions.Paginator);
        functions.Add(nameof(TemplatorRegisterFunctions.Req), TemplatorRegisterFunctions.Req);
        functions.Add(nameof(TemplatorRegisterFunctions.CalendarRow), TemplatorRegisterFunctions.CalendarRow);
        functions.Add(nameof(TemplatorRegisterFunctions.RenderPostContent), TemplatorRegisterFunctions.RenderPostContent);
    }

    #region Front middlewares

    static void UseSiteEngineMiddlewares(WebApplication app)
    {
        UseRobotsTxt(app);
        app.Use(AppendMarsAppFrontInRequestContextItems);
        app.Use(FrontRequestHandlersMiddleware);
        app.Use(FrontStaticFilesMiddleware);

        app.MapFallback("/api/{**slug}", ApiFallbackAsync);

        // Не endpoint: глобальный MapFallback выбирался бы внешним UseRouting для всех не-файл путей
        // и перехватывал /dev (админка) и прочие ветки с их локальными fallback'ами.
        // Middleware стоит последним и рендерит фронт, только если запрос никому не принадлежит.
        app.Use(FrontRenderFallbackMiddleware);
    }

    [DebuggerStepThrough]
    static Task AppendMarsAppFrontInRequestContextItems(HttpContext context, Func<Task> next)
    {
        try
        {
            //TODO: try catch выглядит тяжелой, просто упрозднить
            var locator = context.RequestServices.GetRequiredService<IWebRenderEngineLocator>();
            var appFront = locator.GetAppFrontForUrl(context.Request.Path);
            if (appFront is not null)
                context.Items[nameof(MarsAppFront)] = appFront;
        }
        catch (Exception ex)
        {
            // ошибка создания движка не должна ломать админку/API
            Console.Error.WriteLine($"StartupFront: resolve front error: {ex.Message}");
        }

        return next.Invoke();
    }

    /// <summary>
    /// Пайплайн обработчиков фронтов (IFrontRequestHandler из DI, по возрастанию Order).
    /// Исполняется до статики фронтов и fallback-рендера: обработчик может перехватить запрос
    /// (например, режим обслуживания), не встраиваясь в код фронтов.
    ///
    /// Запросы с endpoint'ом проходят через обработчики только если endpoint помечен
    /// FrontRenderEndpointAttribute (публичное API рендера фронтов); админка и прочие API
    /// не затрагиваются. Файловые запросы (ассеты) идут мимо, кроме html-страниц в wwwroot.
    /// </summary>
    static async Task FrontRequestHandlersMiddleware(HttpContext context, Func<Task> next)
    {
        if (context.GetEndpoint() is not null)
        {
            if (context.GetEndpoint().Metadata.GetMetadata<FrontRenderEndpointAttribute>() is null)
            {
                await next();
                return;
            }
        }
        else if (IsFileRequest(context.Request.Path) && !IsHtmlFileRequest(context.Request.Path))
        {
            await next();
            return;
        }

        var appFront = context.Items[nameof(MarsAppFront)] as MarsAppFront;
        if (appFront is null)
        {
            // API рендера: SetupAppFront контроллера ещё не исполнялся — резолвим фронт сами
            var locator = context.RequestServices.GetRequiredService<IWebRenderEngineLocator>();
            appFront = locator.GetAppFrontForUrl(context.Request.Path) ?? locator.GetAppFrontForUrl("/");
            if (appFront is null)
            {
                await next();
                return;
            }
        }

        var handlers = context.RequestServices.GetRequiredService<IEnumerable<IFrontRequestHandler>>();
        foreach (var handler in handlers.OrderBy(s => s.Order))
        {
            if (await handler.HandleAsync(context, appFront, context.RequestAborted))
                return;
        }

        await next();
    }

    /// <summary>
    /// Статика wwwroot фронтов. До endpoints — как UseStaticFiles в старой схеме.
    /// (Fallback-запросы с точкой в пути — файлы — пропускаются и тут, и в FrontRenderFallbackMiddleware)
    /// </summary>
    static async Task FrontStaticFilesMiddleware(HttpContext context, Func<Task> next)
    {
        // системные роуты никогда не обслуживаются из wwwroot фронтов:
        // защита от shadowing системных ассетов файлами фронтов + минус лишняя проверка на запрос
        if (IsSystemPath(context.Request.Path))
        {
            await next();
            return;
        }

        try
        {
            var locator = context.RequestServices.GetRequiredService<IWebRenderEngineLocator>();
            var appFront = locator.GetAppFrontForUrl(context.Request.Path);
            if (appFront is not null)
            {
                context.Items.TryAdd(nameof(MarsAppFront), appFront);

                if (await locator.TryServeStaticFileAsync(context, appFront))
                    return;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"StartupFront: serve front static file error: {ex.Message}");
        }

        await next();
    }

    static readonly string[] SystemPathPrefixes = ["/dev", "/_content", "/_framework", "/mars", "/api", "/_ws"];

    static bool IsSystemPath(PathString path)
    {
        var value = path.Value ?? "";
        foreach (var prefix in SystemPathPrefixes)
        {
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    static Task ApiFallbackAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return context.Response.WriteAsJsonAsync(new { Ok = false, Message = "ApiNotFound" });
    }

    static async Task FrontRenderFallbackMiddleware(HttpContext context, Func<Task> next)
    {
        // запрос уже принадлежит endpoint'у (контроллер, хаб, страница, robots.txt, /api-fallback,
        // локальный fallback админки) — исполнится терминальным middleware
        if (context.GetEndpoint() is not null)
        {
            await next();
            return;
        }

        // семантика ограничения :nonfile — файловые запросы не рендерим
        if (IsFileRequest(context.Request.Path))
        {
            await next();
            return;
        }

        try
        {
            var locator = context.RequestServices.GetRequiredService<IWebRenderEngineLocator>();

            var appFront = locator.GetAppFrontForUrl(context.Request.Path);
            if (appFront is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsync("Front not found");
                return;
            }

            context.Items.TryAdd(nameof(MarsAppFront), appFront);

            var webSiteProcessor = context.RequestServices.GetRequiredService<IWebSiteProcessor>();
            await webSiteProcessor.Response(context, context.RequestAborted);
        }
        catch (Exception ex)
        {
            // ошибки сборки движка (нет папки фронта, битый шаблон) не должны улетать
            // в глобальный обработчик без сообщения
            Console.Error.WriteLine($"StartupFront: render front error: {ex.Message}");

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync($"<pre>Front render error: {ex.Message}</pre>");
            }
        }
    }

    static bool IsFileRequest(PathString path)
    {
        var value = path.Value ?? "";
        var lastSegmentStart = value.LastIndexOf('/') + 1;
        return value.IndexOf('.', lastSegmentStart) >= 0;
    }

    static bool IsHtmlFileRequest(PathString path)
    {
        var ext = Path.GetExtension(path.Value ?? "");
        return ext.Equals(".html", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".htm", StringComparison.OrdinalIgnoreCase);
    }

    static void UseRobotsTxt(WebApplication app)
    {
        app.Map("robots.txt", (HttpContext context) =>
        {
            context.Response.StatusCode = 200;
            return context.RequestServices.GetRequiredService<IOptionService>().GetOption<SEOOption>().RobotsTxt;
        }).ShortCircuit();
    }

    #endregion

    #region Favicon

    static readonly SemaphoreSlim _faviconLock = new(1, 1);

    static async Task OnChangeFaviconOption(FaviconOption opt, IServiceProvider rootServiceProvider)
    {
        using var scope = rootServiceProvider.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var messageService = serviceProvider.GetRequiredService<IDevAdminConnectionService>();

        if (!await _faviconLock.WaitAsync(0))
        {
            _ = messageService.ShowNotifyMessageForAll("Favicons generation is already in progress", MessageIntent.Warning);
            return;
        }

        var faviconHandler = serviceProvider.GetRequiredService<SiteFaviconConfiguratorHandler>();
        try
        {
            await faviconHandler.Handle(opt, CancellationToken.None);
            ClearCacheAllSiteScriptsBuilders(serviceProvider);
            _ = messageService.ShowNotifyMessageForAll("Favicons generated successfully", MessageIntent.Success);
        }
        catch (Exception ex)
        {
            _ = messageService.ShowNotifyMessageForAll("Error generating favicons: " + ex.Message, MessageIntent.Error);
        }
        finally
        {
            _faviconLock.Release();
        }
    }

    #endregion

    #region Site scripts builders

    static void ClearCacheAllSiteScriptsBuilders(IServiceProvider serviceProvider)
    {
        var appAdminBuilder = serviceProvider.GetRequiredKeyedService<ISiteScriptsBuilder>(AppAdminConstants.SiteScriptsBuilderKey);
        var appFrontBuilder = serviceProvider.GetRequiredKeyedService<ISiteScriptsBuilder>(AppFrontConstants.SiteScriptsBuilderKey);
        appAdminBuilder.ClearCache();
        appFrontBuilder.ClearCache();
    }

    static void AddSiteScriptsBuilders(IServiceCollection services)
    {
        services.AddKeyedSingleton<ISiteScriptsBuilder, SiteScriptsBuilder>(AppAdminConstants.SiteScriptsBuilderKey);
        services.AddKeyedSingleton<ISiteScriptsBuilder, SiteScriptsBuilder>(AppFrontConstants.SiteScriptsBuilderKey);

        services.AddKeyedSingleton<IWebSitePluggablePluginScripts, AppAdminWebSitePluggablePluginScripts>(AppAdminConstants.SiteScriptsBuilderKey);
        services.AddKeyedSingleton<IWebSitePluggablePluginScripts, AppFrontWebSitePluggablePluginScripts>(AppFrontConstants.SiteScriptsBuilderKey);
    }

    static void UseSiteScriptsBuilders(IServiceProvider serviceProvider)
    {
        //Mars.Admin
        {
            // core
            var appAdminBuilder = serviceProvider.GetRequiredKeyedService<ISiteScriptsBuilder>(AppAdminConstants.SiteScriptsBuilderKey);
            appAdminBuilder.RegisterProvider("favicon", new FaviconAssetProvider(serviceProvider.GetRequiredService<IOptionService>()), order: 8f, placeInHead: true);
            var appAdminSpaHtmlScripts = new AppAdminSpaHtmlScripts();
            appAdminBuilder.RegisterProvider("appadmin_head", new AppAdminHeadAssetProvider(appAdminSpaHtmlScripts), order: 9f, placeInHead: true);
            appAdminBuilder.RegisterProvider("appadmin_footer", new AppAdminFooterAssetProvider(appAdminSpaHtmlScripts), order: 9f, placeInHead: false);

            // pluggable
            var appAdminWebSitePluggablePluginScripts = serviceProvider.GetRequiredKeyedService<IWebSitePluggablePluginScripts>(AppAdminConstants.SiteScriptsBuilderKey);
            appAdminBuilder.RegisterProvider("appadmin_scripts_head", new WebSitePluggableHeaderAssetProvider(appAdminWebSitePluggablePluginScripts), order: 10, placeInHead: true);
            appAdminBuilder.RegisterProvider("appadmin_scripts_footer", new WebSitePluggableFooterAssetProvider(appAdminWebSitePluggablePluginScripts), order: 10, placeInHead: false);
        }

        //AppFront
        {
            // core
            var appFrontBuilder = serviceProvider.GetRequiredKeyedService<ISiteScriptsBuilder>(AppFrontConstants.SiteScriptsBuilderKey);
            appFrontBuilder.RegisterProvider("favicon", new FaviconAssetProvider(serviceProvider.GetRequiredService<IOptionService>()), order: 9f, placeInHead: true);

            // pluggable
            var appFrontWebSitePluggablePluginScripts = serviceProvider.GetRequiredKeyedService<IWebSitePluggablePluginScripts>(AppFrontConstants.SiteScriptsBuilderKey);
            appFrontBuilder.RegisterProvider("appfront_scripts_head", new WebSitePluggableHeaderAssetProvider(appFrontWebSitePluggablePluginScripts), order: 10, placeInHead: true);
            appFrontBuilder.RegisterProvider("appfront_scripts_footer", new WebSitePluggableFooterAssetProvider(appFrontWebSitePluggablePluginScripts), order: 10, placeInHead: false);
        }

    }

    #endregion
}
