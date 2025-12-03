namespace Mars.UseStartup;

internal static class StartupHostFiles
{
    public static IApplicationBuilder UseHostFiles(this IApplicationBuilder app)
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            //ServeUnknownFileTypes = true
        });
        return app;
    }
}
