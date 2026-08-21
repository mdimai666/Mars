using Microsoft.AspNetCore.Rewrite;

namespace Mars.UseStartup;

internal static class StartupDevAdmin
{
    public static IApplicationBuilder UseDevAdmin(this IApplicationBuilder app)
    {
#if NOADMIN
        return app;
#endif

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

        return app;
    }
}
