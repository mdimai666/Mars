using Mars.SiteEngine.Abstractions.WebSite;
using Mars.SiteEngine.Scriban.Extensions;
using Mars.TemplateEngine.Providers.ScribanProvider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mars.SiteEngine.Scriban;

public static class MainSiteEngineScriban
{
    public static IServiceCollection AddMarsSiteEngineScriban(this IServiceCollection services)
    {
        services.TryAddSingleton<IScribanEngineFactory, ScribanEngineFactory>();
        services.AddSingleton<IScribanObjectContributor, SiteScribanFunctionsContributor>();
        services.AddSingleton<IWebRenderEngineFactory, ScribanRenderEngineFactory>();

        return services;
    }
}
