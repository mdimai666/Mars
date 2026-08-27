using Mars.Host.Shared.Features;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Managers.Extensions;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.SSO.Services;
using Mars.Options.Models;
using Mars.SSO.Middlewares;
using Mars.SSO.Providers;
using Mars.SSO.Services;
using Mars.SSO.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace Mars.SSO;

public static class MainSSO
{
    public static IServiceCollection AddMarsSSO(this IServiceCollection services)
    {
        services.AddSingleton<OidcMetadataCache>();
        services.AddScoped<ILocalJwtService, MarsLocalJwtService>();
        services.AddSingleton<ITokenCache, MemoryTokenCache>();
        services.AddScoped<DynamicSsoProviderFactory>();
        services.AddSingleton<ISsoProviderRepository, SsoOptionsProviderRepository>();

        services.AddScoped<ISsoService, SsoService>();

        return services;
    }

    public static IApplicationBuilder UseMarsSSOMiddlewares(this IApplicationBuilder app)
    {
        return app.UseMiddlewareForFeature<SsoAuthMiddleware>(FeatureFlags.SingleSignOn);
    }

    public static IServiceProvider UseMarsSSO(this IServiceProvider services)
    {
        _memoryCache = services.GetRequiredService<IMemoryCache>();
        //var op = app.Services.GetRequiredService<IOptionService>();
        var eventManager = services.GetRequiredService<IEventManager>();
        var eventTopic = eventManager.Defaults.OptionUpdate(typeof(OpenIDClientOption).Name);
        //op.on
        eventManager.AddEventListener(eventTopic, OnSSOOptionUpdate);

        var optionService = services.GetRequiredService<IOptionService>();

        optionService.RegisterOption<OpenIDClientOption>(x => ChangeOpenIDClientOption(x, optionService));
        var openIdClient = optionService.GetOption<OpenIDClientOption>();
        ChangeOpenIDClientOption(openIdClient, optionService);

        return services;
    }

    static IMemoryCache _memoryCache = default!;

    static void OnSSOOptionUpdate(ManagerEventPayload _)
    {
        //_memoryCache.Remove("sso:providers:instances");
        _memoryCache.Remove("sso:providers:descriptors");
    }

    static void ChangeOpenIDClientOption(OpenIDClientOption opt, IOptionService optionService)
    {
        var ssoOpt = new AuthVariantConstOption
        {
            SSOConfigs = opt.OpenIDClientConfigs.Where(s => s.Enable).Select(s => new AuthVariantConstOption.SSOProviderInfo
            {
                IconUrl = s.IconUrl,
                Label = s.Title,
                Slug = s.Slug,
                Driver = s.Driver,
            }).ToList()
        };
        optionService.SetConstOption(ssoOpt, appendToInitialSiteData: true);
    }
}
