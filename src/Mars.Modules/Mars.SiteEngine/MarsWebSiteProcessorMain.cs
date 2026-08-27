using Mars.Host.Handlers;
using Mars.Host.Shared.Constants.Website;
using Mars.Host.Shared.Handlers;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite.Scripts;
using Mars.Host.Templators;
using Mars.Host.WebSite.Scripts;
using Mars.WebSiteProcessor.Interfaces;
using Mars.WebSiteProcessor.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebSiteProcessor;

public static class MarsWebSiteProcessorMain
{
    public static IServiceCollection AddMarsWebSiteProcessor(this IServiceCollection services)
    {
        services.AddSingleton<IWebRenderEngineLocator, WebRenderEngineLocator>();
        services.AddSingleton<ITemplatorFeaturesLocator, TemplatorFeaturesLocator>();

        services.AddScoped<IFaviconGeneratorHandler, FaviconGeneratorHandler>();
        services.AddScoped<SiteFaviconConfiguratorHandler>();

        AddSiteScriptsBuilders(services);

        return services;
    }

    public static IApplicationBuilder UseMarsWebSiteProcessor(this WebApplication app)
    {
        UseSiteScriptsBuilders(app.Services);

        return app;
    }

    static void AddSiteScriptsBuilders(IServiceCollection services)
    {
        services.AddKeyedSingleton<ISiteScriptsBuilder, SiteScriptsBuilder>(AppAdminConstants.SiteScriptsBuilderKey);
        services.AddKeyedSingleton<ISiteScriptsBuilder, SiteScriptsBuilder>(AppFrontConstants.SiteScriptsBuilderKey);

        services.AddKeyedSingleton<IWebSitePluggablePluginScripts, AppAdminWebSitePluggablePluginScripts>(AppAdminConstants.SiteScriptsBuilderKey);
        services.AddKeyedSingleton<IWebSitePluggablePluginScripts, AppFrontWebSitePluggablePluginScripts>(AppFrontConstants.SiteScriptsBuilderKey);
    }

    static void UseSiteScriptsBuilders(IServiceProvider serviceProvider)
    {
        //AppAdmin
        {
            // core
            var appAdminBuilder = serviceProvider.GetRequiredKeyedService<ISiteScriptsBuilder>(AppAdminConstants.SiteScriptsBuilderKey);
            appAdminBuilder.RegisterProvider("favicon", new FaviconAssetProvider(serviceProvider.GetRequiredService<IOptionService>()), order: 8f, placeInHead: true);
            var appAdminSpaHtmlScripts = new AppAdminSpaHtmlScripts();
            appAdminBuilder.RegisterProvider("appadmin_head", new AppAdminHeadAssetProvider(appAdminSpaHtmlScripts), order: 9f, placeInHead: true);
            appAdminBuilder.RegisterProvider("appadmin_footer", new AppAdminFooterAssetProvider(appAdminSpaHtmlScripts), order: 9f, placeInHead: false);

            // pluggable
            var appAdminWebSitePluggablePluginScripts = serviceProvider.GetRequiredKeyedService<IWebSitePluggablePluginScripts>(AppAdminConstants.SiteScriptsBuilderKey);
            appAdminBuilder.RegisterProvider("appadmin_scripts_head", new WebSitePluggableHeaderAssetProvider(appAdminWebSitePluggablePluginScripts), order: 10, placeInHead: true);
            appAdminBuilder.RegisterProvider("appadmin_scripts_footer", new WebSitePluggableFooterAssetProvider(appAdminWebSitePluggablePluginScripts), order: 10, placeInHead: false);
        }

        //AppFront
        {
            // core
            var appFrontBuilder = serviceProvider.GetRequiredKeyedService<ISiteScriptsBuilder>(AppFrontConstants.SiteScriptsBuilderKey);
            appFrontBuilder.RegisterProvider("favicon", new FaviconAssetProvider(serviceProvider.GetRequiredService<IOptionService>()), order: 9f, placeInHead: true);

            // pluggable
            var appFrontWebSitePluggablePluginScripts = serviceProvider.GetRequiredKeyedService<IWebSitePluggablePluginScripts>(AppFrontConstants.SiteScriptsBuilderKey);
            appFrontBuilder.RegisterProvider("appfront_scripts_head", new WebSitePluggableHeaderAssetProvider(appFrontWebSitePluggablePluginScripts), order: 10, placeInHead: true);
            appFrontBuilder.RegisterProvider("appfront_scripts_footer", new WebSitePluggableFooterAssetProvider(appFrontWebSitePluggablePluginScripts), order: 10, placeInHead: false);
        }

    }
}
