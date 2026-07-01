using AppFront.Shared.AuthProviders;
using AppFront.Shared.Services;
using Blazored.LocalStorage;
using BlazoredHtmlRender;
using Flurl.Http;
using Mars.Shared.Interfaces;
using Mars.Shared.Tools;
using Mars.WebApiClient;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AppFront.Shared;

public static class MainAppFrontShared
{

    public static void AddAppFront(this IServiceCollection services, IConfiguration configuration, Type program)
    {
        if (!OperatingSystem.IsBrowser()) return;

        ArgumentNullException.ThrowIfNull(services);

        if (!services.Any(d => d.ServiceType == typeof(HttpClient))
            || !services.Any(d => d.ServiceType == typeof(IFlurlClient)))
        {
            throw new InvalidOperationException("HttpClient and IFlurlClient must be registered.");
        }

        Q.Program = program;

        services.AddBlazoredLocalStorage();
        services.AddAuthorizationCore();
        services.TryAddScoped<IAuthenticationService, AuthenticationService>();
        services.TryAddScoped<CookieOrLocalStorageAuthStateProvider>();
        services.TryAddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<CookieOrLocalStorageAuthStateProvider>());

        services.ConfigureLocalizer();

        services.TryAddScoped<ViewModelService>();
        services.TryAddScoped<AppFrontJs>();

        services.TryAddSingleton<ModelInfoService>();
        services.TryAddScoped<DeveloperControlService>();
        //services.TryAddScoped<GalleryService>();
        services.TryAddScoped<IActAppService, ActAppService>();
        services.TryAddScoped<IAIToolAppService, AIToolAppService>();

        services.AddMarsWebApiClient();

        //builder.Logging.SetMinimumLevel(LogLevel.Error);

        BlazoredHtml.AddComponentsFromAssembly(Q.Program.Assembly, true);
        BlazoredHtml.AddComponentsFromAssembly(typeof(AppFront.Shared.Components.LikeButton).Assembly, true);
    }

    private static void ConfigureLocalizer(this IServiceCollection services)
    {
        services.AddLocalization();
        //Такое писать не требуется. Оставлено для внимания.
        //services.TryAddSingleton<IStringLocalizer, StringLocalizer<AppRes>>();
        //services.TryAddSingleton<IStringLocalizer<AppRes>, StringLocalizer<AppRes>>();
    }
}
