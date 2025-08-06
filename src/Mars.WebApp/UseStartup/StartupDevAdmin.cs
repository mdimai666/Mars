using Microsoft.AspNetCore.Rewrite;

namespace Mars.UseStartup;

internal static class StartupDevAdmin
{
    public static IServiceCollection AddDevAdmin(this IServiceCollection services)
    {
#if NOADMIN
        return services;
#endif

        return services;
    }

    public static IApplicationBuilder UseDevAdmin(this IApplicationBuilder app)
    {
#if NOADMIN
        return app;
#endif

        app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/dev"), first =>
        {
            var options = new RewriteOptions()
                //.AddRedirect("(.*)/$", "$1")                // удаление концевого слеша
                //.AddRedirect("(?i)home[/]?$", "home/index"); // переадресация с home на home/index
                //.AddRewrite("^dev/_content/(.*)", "_content/$1", true)
                //.AddRewrite("^dev/_framework/(.*)", "_framework/$1", true)
                //.AddRewrite("^dev/monaco(.*)", "dev/monaco$1", true)
                //.AddRewrite("^dev/(?!_content/)(.*)", "dev/$1", false);
                //.AddRewrite("^dev/monaco(.*)", "dev/monaco$1", true)
                .AddRewrite("^dev/_content/(.*)", "_content/$1", false);

            first.UseRewriter(options);
            first.UseBlazorFrameworkFiles("/dev");

            first.UseStaticFiles();
            first.UseRouting();

            first.UseAuthorization();

            first.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                ////////IList<IRouter> aa;
                ////////aa.

                //endpoints.

                //endpoints.MapGet("/dd", async context =>
                //{
                //    await context.Response.WriteAsJsonAsync("dd");
                //});

                //endpoints.MapFallbackToFile("AppAdmin/{*path:nonfile}", "AppAdmin/index.html");
                endpoints.MapFallbackToPage("/_AdminHost");

                //endpoints.MapFallback(async (req) => {
                //    await req.Response.WriteAsync("Ok");
                //});
            });
        });

        return app;
    }

}
