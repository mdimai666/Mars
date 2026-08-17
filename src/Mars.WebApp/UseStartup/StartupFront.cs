using System.Diagnostics;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite;
using Mars.Services;
using Mars.UseStartup.MarsParts;
using Mars.WebSiteProcessor.Handlebars;
using Mars.WebSiteProcessor.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.UseStartup;

public static class StartupFront
{
    public static WebApplicationBuilder AddFront(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IFrontManager, FrontManager>();
        builder.Services.AddSingleton<FrontTemplateService>();
        builder.Services.AddSingleton<IFrontFilesService, FrontFilesService>();
        builder.Services.AddSingleton<FrontRenderWarmupService>();

        builder.AddWREHandlebars();

        return builder;
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

    public static IApplicationBuilder UseFront(this WebApplication app)
    {
        // специальный фронт админки (data/admin/front) — создаётся один раз при старте
        app.Services.GetRequiredService<FrontTemplateService>().EnsureAdminFront();

        UseRobotsTxt(app);
        app.Use(AppendMarsAppFrontInRequestContextItems);
        app.Use(FrontRequestHandlersMiddleware);
        app.Use(FrontStaticFilesMiddleware);

        app.MapFallback("/api/{**slug}", ApiFallbackAsync);

        // Не endpoint: глобальный MapFallback выбирался бы внешним UseRouting для всех не-файл путей
        // и перехватывал /dev (админка) и прочие ветки с их локальными fallback'ами.
        // Middleware стоит последним и рендерит фронт, только если запрос никому не принадлежит.
        app.Use(FrontRenderFallbackMiddleware);

        return app;
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
        var endpoint = context.GetEndpoint();
        if (endpoint is not null)
        {
            if (endpoint.Metadata.GetMetadata<FrontRenderEndpointAttribute>() is null)
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
            return context.RequestServices.GetRequiredService<IOptionService>().RobotsTxt();
        }).ShortCircuit();
    }

}
