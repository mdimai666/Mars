using Mars.Host.Shared.Models;
using Mars.Host.Shared.Templators;
using Mars.Host.Templators.HandlebarsFunc;
using Mars.WebSiteProcessor.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebSiteProcessor.Handlebars;

public static class MainHandlebarsWRE
{
    public static WebApplicationBuilder AddWREHandlebars(this WebApplicationBuilder builder, IReadOnlyCollection<MarsAppFront> apps)
    {
        builder.Services.AddTransient<IMarsHtmlTemplator, MyHandlebars>();

        return builder;
    }

    public static WebApplication UseWREHandlebars(this WebApplication app, IReadOnlyCollection<MarsAppFront> apps)
    {
        foreach (var appFront in apps.Where(app => app.Configuration.Mode == Core.Models.AppFrontMode.HandlebarsTemplateStatic))
        {
            var renderEngine = ActivatorUtilities.CreateInstance<HandlebarsWebRenderEngine>(app.Services, appFront);
            appFront.Features.Set<IWebRenderEngine>(renderEngine);

            //string localizeFile = Path.Combine(app.Configuration.Path, "Resources", "AppRes.resx");

            //if (File.Exists(localizeFile))
            //{
            //    var resLoaderFactory = new LocalizerXmlResLoaderFactory(localizeFile);
            //    builder.Services.AddSingleton<IAppFrontLocalizer>(resLoaderFactory);
            //}
        }

        return app;
    }
}
