using Mars.Options.Abstractions.Services;
using Mars.Server.Abstractions.Features;
using Mars.Server.Abstractions.Managers;
using Mars.Server.Abstractions.Managers.Extensions;
using Mars.SSO.Abstractions.Services;
using Mars.SSO.Contracts.Options;
using Mars.SSO.Host.Middlewares;
using Mars.SSO.Host.Providers;
using Mars.SSO.Host.Services;
using Mars.SSO.Host.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace Mars.SSO.Host;

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

    /// <summary>
    /// Вызывается между UseAuthentication и UseAuthorization: SsoAuthMiddleware
    /// подменяет principal по внешнему токену до проверки авторизации.
    /// </summary>
    public static IApplicationBuilder UseMarsSSO(this WebApplication app)
    {
        app.UseMiddlewareForFeature<SsoAuthMiddleware>(FeatureFlags.SingleSignOn);

        var services = app.Services;
        var memoryCache = services.GetRequiredService<IMemoryCache>();
        var eventManager = services.GetRequiredService<IEventManager>();
        var eventTopic = eventManager.Defaults.OptionUpdate(typeof(OpenIDClientOption).Name);
        eventManager.AddEventListener(eventTopic, _ => memoryCache.Remove("sso:providers:descriptors"));

        var optionService = services.GetRequiredService<IOptionService>();

        optionService.RegisterOption<OpenIDClientOption>(x => ChangeOpenIDClientOption(x, optionService));
        var openIdClient = optionService.GetOption<OpenIDClientOption>();
        ChangeOpenIDClientOption(openIdClient, optionService);

        return app;
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
