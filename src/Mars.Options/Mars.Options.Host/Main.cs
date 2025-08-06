using Mars.Host.Shared.Services;
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

}
