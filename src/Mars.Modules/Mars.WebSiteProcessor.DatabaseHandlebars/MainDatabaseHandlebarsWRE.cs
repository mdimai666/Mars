using Mars.Core.Models;
using Mars.Host.Shared.Models;
using Mars.WebSiteProcessor.Interfaces;
using Microsoft.AspNetCore.Builder;

namespace Mars.WebSiteProcessor.DatabaseHandlebars;

public static class MainDatabaseHandlebarsWRE
{
    public static WebApplicationBuilder AddWREDatabaseHandlebars(this WebApplicationBuilder builder, IReadOnlyCollection<MarsAppFront> apps)
    {
        foreach (var app in apps.Where(app => app.Configuration.Mode is AppFrontMode.HandlebarsTemplate or AppFrontMode.None))
        {
            var renderEngine = new DatabaseHandlebarsWebRenderEngine();
            app.Features.Set<IWebRenderEngine>(renderEngine);
        }
        return builder;
    }

    public static IApplicationBuilder UseWREDatabaseHandlebars(this IApplicationBuilder app, IReadOnlyCollection<MarsAppFront> apps)
    {

        return app;
    }
}
