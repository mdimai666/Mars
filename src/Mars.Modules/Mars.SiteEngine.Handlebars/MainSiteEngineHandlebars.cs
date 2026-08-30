using Mars.SiteEngine.Abstractions.Templators;
using Mars.SiteEngine.Abstractions.WebSite;
using Mars.SiteEngine.Handlebars.HandlebarsFunc;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.SiteEngine.Handlebars;

public static class MainSiteEngineHandlebars
{
    public static IServiceCollection AddMarsSiteEngineHandlebars(this IServiceCollection services)
    {
        services.AddTransient<IMarsHtmlTemplator, MyHandlebars>();
        services.AddSingleton<IWebRenderEngineFactory, HandlebarsRenderEngineFactory>();

        return services;
    }
}
