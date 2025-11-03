using Mars.Host.Handlers;
using Mars.Host.Shared.Constants.Website;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite.Scripts;
using Mars.Options.Models;
using Mars.Shared.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Options.Host;

public static class MainOptions
{
    static IOptionService optionService = default!;

    public static IServiceCollection AddMarsOptions(this IServiceCollection services)
    {
        return services;
    }

    public static IServiceProvider UseMarsOptions(this IServiceProvider serviceProvider)
    {
        optionService = serviceProvider.GetRequiredService<IOptionService>();

        optionService.RegisterOption<SysOptions>(ChangeSysOptions);
        optionService.RegisterOption<SmtpSettingsModel>(ChangeMailSettings);

        optionService.RegisterOption<FrontOptions>();
        optionService.RegisterOption<FrontRoutingOption>();
        optionService.RegisterOption<DevAdminStyleOption>(appendToInitialSiteData: true);

        optionService.RegisterOption<ApiOption>();
        optionService.RegisterOption<MediaOption>();
        optionService.RegisterOption<MaintenanceModeOption>();
        optionService.RegisterOption<SEOOption>();
        optionService.RegisterOption<PluginManagerSettingsOption>();
        optionService.RegisterOption<FaviconOption>(opt => _ = OnChangeFaviconOption(opt, serviceProvider));
        optionService.RegisterOption<FaviconOptionGenaratedValues>();

        optionService.GetOption<SysOptions>();
        optionService.GetOption<SmtpSettingsModel>();
        optionService.GetOption<SEOOption>();

        return serviceProvider;
    }

    static void ChangeSysOptions(SysOptions sys)
    {
        //TODO: think about it
        //AppSharedSettings.BackendUrl = sys.SiteUrl.TrimEnd('/');
    }

    static void ChangeMailSettings(SmtpSettingsModel opt)
    {

    }

    private static readonly SemaphoreSlim _faviconLock = new(1, 1);

    static async Task OnChangeFaviconOption(FaviconOption opt, IServiceProvider rootServiceProvider)
    {
        using var scope = rootServiceProvider.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var messageService = serviceProvider.GetRequiredService<IDevAdminConnectionService>();

        if (!await _faviconLock.WaitAsync(0))
        {
            _ = messageService.ShowNotifyMessageForAll("Favicons generation is already in progress", Core.Models.MessageIntent.Warning);
            return;
        }

        var faviconHandler = serviceProvider.GetRequiredService<SiteFaviconConfiguratorHandler>();
        try
        {
            await faviconHandler.Handle(opt, CancellationToken.None);
            ClearCacheAllSiteScriptsBuilders(serviceProvider);
            _ = messageService.ShowNotifyMessageForAll("Favicons generated successfully", Core.Models.MessageIntent.Success);
        }
        catch (Exception ex)
        {
            _ = messageService.ShowNotifyMessageForAll("Error generating favicons: " + ex.Message, Core.Models.MessageIntent.Error);
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
}
