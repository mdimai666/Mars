namespace Mars.UseStartup;

internal static class StartupHostFiles
{
    public static IApplicationBuilder UseHostFiles(this IApplicationBuilder app)
    {
        // бут-ассеты админки (/dev/_framework) физически лежат в wwwroot и отдаются этим
        // UseStaticFiles, минуя ветку /dev. Без Cache-Control браузер закеширует их
        // эвристически и может держать старую версию после деплоя;
        // no-cache + ETag — всегда ревалидация (дёшево, 304)
        app.Use(async (ctx, next) =>
        {
            if (ctx.Request.Path.StartsWithSegments("/dev/_framework"))
                ctx.Response.Headers.CacheControl = "no-cache";
            await next();
        });

        app.UseStaticFiles(new StaticFileOptions
        {
            //ServeUnknownFileTypes = true
        });
        return app;
    }
}
