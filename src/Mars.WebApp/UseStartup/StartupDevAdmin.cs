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
            first.UseBlazorFrameworkFiles("/dev");

            first.UseStaticFiles();
            first.UseRouting();

            first.UseAuthorization();

            first.UseEndpoints(endpoints =>
            {
                endpoints.MapFallbackToPage("/_AdminHost");
            });
        });

        return app;
    }
}
