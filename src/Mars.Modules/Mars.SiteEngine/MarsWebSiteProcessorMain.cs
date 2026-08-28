using Mars.Core.Models;
using Mars.Options.Services;
using Mars.Server.Abstractions.Services;
using Mars.SiteEngine.Abstractions.Services;
using Mars.SiteEngine.Contracts.Options;
using Mars.SiteEngine.Handlers;
using Mars.SiteEngine.Abstractions.Constants.Website;
using Mars.SiteEngine.Handlers;
using Mars.Options.Services;
using Mars.SiteEngine.Abstractions.Services;
using Mars.SiteEngine.Handlers;
using Mars.SiteEngine.Abstractions.WebSite.Scripts;
using Mars.SiteEngine.Templators;
using Mars.SiteEngine.WebSite.Scripts;
using Mars.SiteEngine.Interfaces;
using Mars.SiteEngine.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.SiteEngine;

public static class MarsWebSiteProcessorMain
{
    public static IServiceCollection AddMarsWebSiteProcessor(this IServiceCollection services)
    {
        services.AddSingleton<IWebRenderEngineLocator, WebRenderEngineLocator>();
        services.AddSingleton<ITemplatorFeaturesLocator, TemplatorFeaturesLocator>();
        services.AddSingleton<IFrontManager, FrontManager>();

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

    public static IServiceProvider UseMarsSiteEngineOptions(this IServiceProvider services)
    {
        var optionService = services.GetRequiredService<IOptionService>();
        optionService.RegisterOption<FrontsOption>();
        optionService.RegisterOption<SEOOption>();
        optionService.GetOption<SEOOption>();
        optionService.RegisterOption<FaviconOption>(opt => _ = OnChangeFaviconOption(opt, services));
        optionService.RegisterOption<FaviconOptionGenaratedValues>();
        return services;
    }

    static readonly SemaphoreSlim _faviconLock = new(1, 1);

    static async Task OnChangeFaviconOption(FaviconOption opt, IServiceProvider rootServiceProvider)
    {
        using var scope = rootServiceProvider.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var messageService = serviceProvider.GetRequiredService<IDevAdminConnectionService>();

        if (!await _faviconLock.WaitAsync(0))
        {
            _ = messageService.ShowNotifyMessageForAll("Favicons generation is already in progress", MessageIntent.Warning);
            return;
        }

        var faviconHandler = serviceProvider.GetRequiredService<SiteFaviconConfiguratorHandler>();
        try
        {
            await faviconHandler.Handle(opt, CancellationToken.None);
            ClearCacheAllSiteScriptsBuilders(serviceProvider);
            _ = messageService.ShowNotifyMessageForAll("Favicons generated successfully", MessageIntent.Success);
        }
        catch (Exception ex)
        {
            _ = messageService.ShowNotifyMessageForAll("Error generating favicons: " + ex.Message, MessageIntent.Error);
        }
        finally
        {
            _faviconLock.Release();
        }
    }

    static void ClearCacheAllSiteScriptsBuilders(IServiceProvider serviceProvider)
    {
        var appAdminBuilder = serviceProvider.GetRequiredKeyedService<ISiteScriptsBuilder>(AppAdminConstants.SiteScriptsBuilderKey);
        var appFrontBuilder = serviceProvider.GetRequiredKeyedService<ISiteScriptsBuilder>(AppFrontConstants.SiteScriptsBuilderKey);
        appAdminBuilder.ClearCache();
        appFrontBuilder.ClearCache();
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
        //Mars.Admin
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
