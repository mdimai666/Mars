using AppFront.Shared.Interfaces;
using Mars.Core.Models;
using Mars.Host.Shared.Models;
using Mars.WebSiteProcessor.Blazor.Services;
using Mars.WebSiteProcessor.Handlebars;
using Mars.WebSiteProcessor.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebSiteProcessor.Blazor;

public static class MainBlazorWRE
{
    public static WebApplicationBuilder AddWREBlazor(this WebApplicationBuilder builder, IReadOnlyCollection<MarsAppFront> apps)
    {
        builder.Services.AddScoped<HtmlRenderer>();
        builder.Services.AddScoped<BlazorRenderer>();
        builder.Services.AddScoped<IMarsHostBlazorPrerenderHttpAccessor, MarsHostBlazorPrerenderHttpAccessor>();

        return builder;
    }

    public static WebApplication UseWREBlazor(this WebApplication app, IReadOnlyCollection<MarsAppFront> apps)
    {
        foreach (var appFront in apps.Where(app => app.Configuration.Mode is AppFrontMode.ServeStaticBlazor or AppFrontMode.BlazorPrerender))
        {
            var renderEngine = ActivatorUtilities.CreateInstance<BlazorWebRenderEngine>(app.Services, appFront);
            //var renderEngine = new BlazorWebRenderEngine();
            appFront.Features.Set<IWebRenderEngine>(renderEngine);
        }

        return app;
    }
}
