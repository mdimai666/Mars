using Mars.Core.Models;
using Mars.Host.Shared.Models;
using Mars.WebSiteProcessor.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebSiteProcessor.DatabaseHandlebars;

public static class MainDatabaseHandlebarsWRE
{
    public static WebApplicationBuilder AddWREDatabaseHandlebars(this WebApplicationBuilder builder, IReadOnlyCollection<MarsAppFront> apps)
    {

        return builder;
    }

    public static WebApplication UseWREDatabaseHandlebars(this WebApplication app, IReadOnlyCollection<MarsAppFront> apps)
    {
        foreach (var appFront in apps.Where(app => app.Configuration.Mode is AppFrontMode.HandlebarsTemplate or AppFrontMode.None))
        {
            var renderEngine = ActivatorUtilities.CreateInstance<DatabaseHandlebarsWebRenderEngine>(app.Services, appFront);

            //var renderEngine = new DatabaseHandlebarsWebRenderEngine();
            appFront.Features.Set<IWebRenderEngine>(renderEngine);
        }

        return app;
    }
}
