using Mars.Host.Shared.Models;
using Mars.Host.Shared.WebSite;
using Mars.UseStartup.MarsParts;
using Mars.Core.Models;
using Microsoft.Extensions.FileProviders;

namespace Mars.UseStartup;

public static class StartupStaticHandlebars
{
    public static WebApplicationBuilder AddStaticHandlebarsFront(this WebApplicationBuilder builder, AppFrontSettingsCfg appFront)
    {
        if (appFront.Mode == AppFrontMode.HandlebarsTemplate || string.IsNullOrEmpty(appFront.Path) == false)
        {
            //APP_wwwroot = Path.Combine(appFront.Path);
            //APP_path = Path.Combine(af.Path, "_framework");

        }
        else
        {
            throw new ArgumentNullException("cfg: AppFront.Path");
        }

        return builder;
    }

    public static IApplicationBuilder UseStaticHandlebarsFront(this IApplicationBuilder app, MarsAppFront appFront)
    {
        IWebSiteProcessor webSiteProcessor = app.ApplicationServices.GetRequiredService<IWebSiteProcessor>();

        app.Map(appFront.Configuration.Url, front =>
        {
            front.UseRouting();
            front.UseAuthorization();
            //front.UsePathBase("/app");
            //front.UseBlazorFrameworkFiles("/app");
            front.UseStaticFiles();

            if (appFront.Configuration.Mode != AppFrontMode.HandlebarsTemplate)
            {
                front.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(appFront.Configuration.Path),
                    //RequestPath = new PathString("/app"),
                    ServeUnknownFileTypes = true
                });
            }

            front.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MarsUseEndpointApiFallback();

                endpoints.MapFallback(webSiteProcessor.Response);
            });

        });

        return app;
    }
}
