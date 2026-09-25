using Mars.SiteEngine.Abstractions.WebSite;
using Mars.SiteEngine.Handlebars.Extensions;
using Mars.TemplateEngine.Providers.HandlebarsProvider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mars.SiteEngine.Handlebars;

public static class MainSiteEngineHandlebars
{
    public static IServiceCollection AddMarsSiteEngineHandlebars(this IServiceCollection services)
    {
        services.TryAddSingleton<IHandlebarsEngineFactory, HandlebarsEngineFactory>();
        services.AddSingleton<IHandlebarsBuilderContributor, SiteBasicHelpersContributor>();
        services.AddSingleton<IHandlebarsBuilderContributor, SiteContextHelpersContributor>();
        services.AddSingleton<IWebRenderEngineFactory, HandlebarsRenderEngineFactory>();

        return services;
    }
}
