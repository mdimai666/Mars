using System.Reflection;
using Mars.Admin.Contracts.Options;
using Mars.Admin.Framework;
using Mars.Admin.Host.Handlers;
using Mars.Admin.Host.Services;
using Mars.Admin.Host.XActions;
using Mars.Nodes.Abstractions.Hubs;
using Mars.Options.Abstractions.Services;
using Mars.Server.Abstractions.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Admin.Host;

public static class MainAdmin
{
    public static IServiceCollection AddMarsAdmin(this IServiceCollection services, IConfiguration configuration)
    {
#if !NOADMIN
        services.AddMarsAdminFramework(configuration, typeof(Mars.Admin.App));
#endif

        services.AddSingleton<IDevAdminConnectionService, DevAdminConnectionService>();

        // стартовые данные админки агрегируются на сервере из сервисов модулей
        services.AddScoped<IInitialSiteDataViewModelHandler, InitialSiteDataViewModelHandler>();
        services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());

        // серверные двойники WASM-регистраций (ранний выход по !IsBrowser() в аддон-фронте)
        services.AddAdminFrameworkServerServices();

        // Razor-страницы модуля (_AdminHost) обнаруживаются только как CompiledRazorAssemblyPart —
        // обычный AddApplicationPart даёт AssemblyPart, который маршруты страниц не отдаёт
        services.AddRazorPages().PartManager.ApplicationParts.Add(new CompiledRazorAssemblyPart(typeof(MainAdmin).Assembly));

        return services;
    }

    /// <summary>
    /// Админка: хаб /_ws/admin, кеширование /dev/_framework, ветка /dev
    /// (MapStaticAssets + fallback на SPA), options-формы и XActions админки.
    /// </summary>
    public static IApplicationBuilder UseMarsAdmin(this WebApplication app)
    {
        app.MapHub<ChatHub>("/_ws/admin", options =>
        {
            options.Transports = HttpTransportType.WebSockets | HttpTransportType.LongPolling;
        });

        // бут-ассеты админки (/dev/_framework) физически лежат в wwwroot;
        // без Cache-Control браузер закеширует их эвристически и может держать
        // старую версию после деплоя; no-cache + ETag — всегда ревалидация (дёшево, 304)
        app.Use(async (ctx, next) =>
        {
            if (ctx.Request.Path.StartsWithSegments("/dev/_framework"))
                ctx.Response.Headers.CacheControl = "no-cache";
            await next();
        });

        app.Services.UseMarsAdminFramework();

#if NOADMIN
        return app;
#endif

        app.Services.GetRequiredService<IOptionService>().RegisterOption<DevAdminStyleOption>(appendToInitialSiteData: true);

        app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/dev"), first =>
        {
            // base href админки — "/dev/", поэтому относительные ссылки RCL вида _content/...
            // браузер запрашивает как /dev/_content/... — возвращаем их на корневой _content,
            // где реально лежат статические ассеты
            var options = new RewriteOptions()
                .AddRewrite("^dev/_content/(.*)", "_content/$1", false);

            first.UseRewriter(options);

            first.UseRouting();
            first.UseAuthorization();

            first.UseEndpoints(endpoints =>
            {
                // статические ассеты эндпоинтами: после publish раздаёт прекомпрессированные
                // .br/.gz по Accept-Encoding (без ручного JS-brotli) и ставит Cache-Control
                endpoints.MapStaticAssets();

                endpoints.MapFallbackToPage("/_AdminHost");
            });

            // fallback для ассетов, которых нет в MapStaticAssets (например _framework в Development)
            first.UseBlazorFrameworkFiles("/dev");
            first.UseStaticFiles();
        });

        app.UseMarsAdminXActions();

        return app;
    }
}
