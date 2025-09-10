using Mars.Host.Constants.Website;
using Mars.Host.Handlers;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite.Scripts;
using Mars.Options.Models;
using Mars.Shared.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Options.Host;

public static class MarsOptionsHostExtensions
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

        optionService.RegisterOption<OpenIDClientOption>(ChangeOpenIDClientOption);
        optionService.RegisterOption<OpenIDServerOption>();

        optionService.GetOption<SysOptions>();
        optionService.GetOption<SmtpSettingsModel>();
        optionService.GetOption<SEOOption>();

        var openIdClient = optionService.GetOption<OpenIDClientOption>();
        ChangeOpenIDClientOption(openIdClient);

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

    static void ChangeOpenIDClientOption(OpenIDClientOption opt)
    {
        var ssoOpt = new AuthVariantConstOption
        {
            Variants = AuthVariantConstOption.AuthVariants.LoginPass | AuthVariantConstOption.AuthVariants.SSO,
            SSOConfigs = opt.OpenIDClientConfigs.Where(s => s.Enable).Select(s => new AuthVariantConstOption.SSOProviderInfo
            {
                IconUrl = s.IconUrl,
                Label = s.Title,
                Slug = s.Slug,
            }).ToList()
        };
        optionService.SetConstOption(ssoOpt, appendToInitialSiteData: true);
    }

    private static readonly SemaphoreSlim _faviconLock = new(1, 1);

    static async Task OnChangeFaviconOption(FaviconOption opt, IServiceProvider rootServiceProvider)
    {
        using var scope = rootServiceProvider.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var messageService = serviceProvider.GetRequiredService<IDevAdminConnectionService>();

        if (!await _faviconLock.WaitAsync(0))
        {
            _ = messageService.ShowNotifyMessage("Favicons generation is already in progress", Core.Models.MessageIntent.Warning);
            return;
        }

        var faviconHandler = serviceProvider.GetRequiredService<SiteFaviconConfiguratorHandler>();
        try
        {
            await faviconHandler.Handle(opt, CancellationToken.None);
            ClearCacheAllSiteScriptsBuilders(serviceProvider);
            _ = messageService.ShowNotifyMessage("Favicons generated successfully", Core.Models.MessageIntent.Success);
        }
        catch (Exception ex)
        {
            _ = messageService.ShowNotifyMessage("Error generating favicons: " + ex.Message, Core.Models.MessageIntent.Error);
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
