//using Mars.Areas.Identity;
using Mars.Handlers;
using Mars.Host;
using Mars.Host.Services;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite;
using Mars.MetaModelGenerator;
using Mars.Nodes;
using Mars.Nodes.Core.Implements;
using Mars.QueryLang.Host;
using Mars.Services;
using Mars.WebSiteProcessor.Endpoints;

namespace Mars.UseStartup.MarsParts;

internal static class MarsStartupPartServices
{
    public static IServiceCollection AddMarsHostServices(this IServiceCollection services, IWebHostEnvironment wenv)
    {
        //services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<UserEntity>>();

        //services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.JwtSectionKey));
        services.AddSingleton<ITokenService, TokenService>()
                .AddSingleton<IKeyMaterialService, KeyMaterialService>()
                .AddScoped<AccountsService>()
                .AddScoped<IAccountsService, AccountsService>() //Move to Host
                .AddScoped<IExperimentalSignInService, ExperimentalSignInService>();

        services.AddDatabaseDeveloperPageExceptionFilter();//from clear template

        services.AddMarsQueryLang()
                .AddMetaModelGenerator();

        // basic services
        services.AddSingleton<IMarsSystemService, MarsSystemService>()
                .AddSingleton<IImageProcessor, ImageProcessor>()
                .AddSingleton<IWebSiteProcessor, MapWebSiteProcessor>()
                .AddSingleton<IDevAdminConnectionService, DevAdminConnectionService>()
                .AddScoped<IPageRenderService, PageRenderService>();

        services.AddMarsHost(wenv);

        // additional components
        services.AddSingleton<IAIToolService, AIToolService>();
        services.AddScoped<PostTypePresentationRenderHandler>();

        //services.AddSingleton<DebugService>();

        services.AddSingleton<IServiceCollection>(services);

        return services;
    }

    public static IServiceProvider UseMarsHostServices(this IServiceProvider services)
    {
        var nodeImplementFactory = services.GetRequiredService<NodeImplementFactory>();
        nodeImplementFactory.RegisterAssembly(typeof(MarsHostRootLayoutRenderNodeImpl).Assembly);

        return services;
    }

}
