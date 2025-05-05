using AppFront.Shared.AuthProviders;
using AppFront.Shared.Services;
using Mars.Shared.Interfaces;
using Mars.Shared.Resources;
using Mars.Shared.Tools;
using Mars.WebApiClient;
using MarsEditors;
using Blazored.LocalStorage;
using BlazoredHtmlRender;
using Flurl.Http;
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
        MarsCodeEditor.HostDomain = Q.BackendUrl;


        services.AddBlazoredLocalStorage();
        services.AddAuthorizationCore();
        services.TryAddScoped<IAuthenticationService, AuthenticationService>();
        services.TryAddScoped<AuthenticationService, AuthenticationService>();
        services.TryAddScoped<AuthenticationStateProvider, AuthStateProvider>();

        services.AddHttpClientInterceptor();


        HttpClient client = new HttpClient();

        if (string.IsNullOrEmpty(backendUrl) == false)
        {
            //BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) 
            //BaseAddress = new Uri("http://localhostq:5003")
            //BaseAddress = new Uri(builder.Configuration["BackendUrl"])
            client.BaseAddress = new Uri(backendUrl);
        };

        services.TryAddScoped(sp =>
        {
            client.EnableIntercept(sp);
            return client;
        });

        services.TryAddSingleton<IFlurlClient>(sp => new FlurlClient(client));

        //LocaleProvider.SetLocale("en-US");
        //LocaleProvider.SetLocale("ru-RU", ruRU);
        //services.AddLocalization(options => options.ResourcesPath = "Resources");
        //services.AddLocalization(options => options.ResourcesPath = "Resources");
        services.AddLocalization();
        services.TryAddSingleton<IStringLocalizer, StringLocalizer<AppRes>>();
        services.TryAddSingleton<IStringLocalizer<AppRes>, StringLocalizer<AppRes>>();

        services.TryAddScoped<ViewModelService>();

        //services.TryAddScoped<GeoLocationService>();
        //services.TryAddScoped<GeoLocationTypeService>();
        //services.TryAddScoped<GeoMunicipalityService>();
        //services.TryAddScoped<GeoMunicTypeService>();
        //services.TryAddScoped<GeoRegionService>();
        //services.TryAddScoped<GeoRegionCenterService>();

        services.TryAddScoped<ModelInfoService>();
        services.TryAddScoped<DeveloperControlService>();
        //services.TryAddScoped<GalleryService>();

        //ANKETA
        //services.TryAddScoped<AnketaQuestionService>();
        //services.TryAddScoped<AnketaAnswerService>();
        //services.TryAddScoped<StoEntityTypeService>();
        //services.TryAddScoped<AppDebugService>();
        services.TryAddScoped<IActAppService, ActAppService>();

        services.AddMarsWebApiClient();

        //builder.Logging.SetMinimumLevel(LogLevel.Error);

        services.TryAddScoped(sp =>
        {
            Q.AddSrvProv(sp);
            return new Q();
        });

        BlazoredHtml.AddComponentsFromAssembly(Q.Program.Assembly, true);
        BlazoredHtml.AddComponentsFromAssembly(typeof(AppFront.Shared.Components.LikeButton).Assembly, true);
    }
}
