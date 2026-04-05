using AppFront.Shared.AuthProviders;
using AppFront.Shared.Services;
using Blazored.LocalStorage;
using BlazoredHtmlRender;
using Flurl.Http;
using Mars.Shared.Interfaces;
using Mars.Shared.Resources;
using Mars.Shared.Tools;
using Mars.WebApiClient;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Toolbelt.Blazor.Extensions.DependencyInjection;

namespace AppFront.Shared;

public static class AppFrontSharedExtensions
{

    public static void AddAppFront(this IServiceCollection services, IConfiguration configuration, Type program)
    {
        if (services == null)
        {
            throw new ArgumentNullException("services");
        }

        //Microsoft.AspNetCore.Components.WebAssembly.Hosting.WebAssemblyHostConfiguration

        string backendUrl = Q.BackendUrl;

        if (string.IsNullOrEmpty(configuration["BackendUrl"]) == false)
        {
            backendUrl = configuration["BackendUrl"].TrimEnd('/');
            Q.BackendUrl = backendUrl;
        }

        Q.Program = program;
        //MarsCodeEditor.HostDomain = Q.BackendUrl;

        services.AddBlazoredLocalStorage();
        services.AddAuthorizationCore();
        services.TryAddScoped<IAuthenticationService, AuthenticationService>();
        services.TryAddScoped<CookieOrLocalStorageAuthStateProvider>();
        services.TryAddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<CookieOrLocalStorageAuthStateProvider>());

        services.AddHttpClientInterceptor();

        var client = new HttpClient();

        if (string.IsNullOrEmpty(backendUrl) == false)
        {
            //BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
            //BaseAddress = new Uri("http://localhostq:5003")
            //BaseAddress = new Uri(builder.Configuration["BackendUrl"])
            client.BaseAddress = new Uri(backendUrl);
        }

        services.TryAddScoped(sp =>
        {
            client.EnableIntercept(sp);
            return client;
        });

        services.TryAddSingleton<IFlurlClient>(sp => new FlurlClient(client));

        services.ConfigureLocalizer();

        services.TryAddScoped<ViewModelService>();

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
