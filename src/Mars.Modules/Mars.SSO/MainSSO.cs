using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Managers.Extensions;
using Mars.Host.Shared.SSO.Services;
using Mars.Options.Models;
using Mars.SSO.Middlewares;
using Mars.SSO.Providers;
using Mars.SSO.Services;
using Mars.SSO.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.SSO;

public static class MainSSO
{
    public static IServiceCollection AddMarsSSO(this IServiceCollection services)
    {
        services.AddSingleton<OidcMetadataCache>();
        services.AddScoped<ILocalJwtService, MarsLocalJwtService>();
        services.AddSingleton<ITokenCache, MemoryTokenCache>();
        services.AddScoped<DynamicSsoProviderFactory>();
        services.AddScoped<ISsoProviderRepository, SsoOptionsProviderRepository>();

        //services.AddSingleton<ISsoProvider, LocalJwtProvider>();

        services.AddScoped<ISsoService, SsoService>();
        //services.AddTransient<ISsoProvider, KeycloakProvider2>();
        //services.AddControllers();

        return services;
    }

    public static IApplicationBuilder UseMarsSSOMiddlewares(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SsoAuthMiddleware>();
    }

    public static IApplicationBuilder UseMarsSSO(this WebApplication app)
    {
        _memoryCache = app.Services.GetRequiredService<IMemoryCache>();
        //var op = app.Services.GetRequiredService<IOptionService>();
        var eventManager = app.Services.GetRequiredService<IEventManager>();
        var eventTopic = eventManager.Defaults.OptionUpdate(typeof(OpenIDClientOption).Name);
        //op.on
        eventManager.AddEventListener(eventTopic, OnSSOOptionUpdate);

        return app;
    }

    static IMemoryCache _memoryCache = default!;

    static void OnSSOOptionUpdate(ManagerEventPayload _)
    {
        //_memoryCache.Remove("sso:providers:instances");
        _memoryCache.Remove("sso:providers:descriptors");
    }
}
