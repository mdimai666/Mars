//using Mars.Areas.Identity;
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
    public static IServiceCollection MarsAddServices(this IServiceCollection services, IWebHostEnvironment wenv)
    {
        //services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<UserEntity>>();

        //services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.JwtSectionKey));
        services.AddScoped<ITokenService, TokenService>()
                .AddScoped<AccountsService>();

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

        //services.AddSingleton<DebugService>();
        services.AddSingleton<IPluginService, PluginService>();

        NodeImplementFabirc.RegisterAssembly(typeof(MarsHostRootLayoutRenderNodeImpl).Assembly);

        services.AddSingleton<IServiceCollection>(services);

        return services;
    }

}
