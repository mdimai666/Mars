//using Mars.Areas.Identity;
//using Mars.GenSourceCode;
using Mars.Host;
using Mars.Host.Services;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite;
using Mars.Nodes;
using Mars.Nodes.Core.Implements;
using Mars.Services;
using Mars.WebSiteProcessor.Endpoints;

namespace Mars.UseStartup.MarsParts;

internal static class MarsStartupPartServices
{
    public static IServiceCollection MarsAddServices(this IServiceCollection services, IWebHostEnvironment wenv)
    {
        //services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<UserEntity>>();

        //services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.JwtSectionKey));
        services.AddScoped<ITokenService, TokenService>();

        services.AddDatabaseDeveloperPageExceptionFilter();//from clear template

        services.AddScoped<AccountsService>();
        services.AddSingleton<IImageProcessor, ImageProcessor>();

        // TODO: uncomment
        //services.AddSingleton<IRuntimeTypeCompiler, RuntimeTypeCompiler>();
        services.AddSingleton<IWebSiteProcessor, MapWebSiteProcessor>();

        services.AddScoped<IPageRenderService, PageRenderService>();
        services.AddScoped<FeedbackService>();
        services.AddScoped<ViewModelService>();

        services.AddMarsHost(wenv);
        services.AddScoped<EsiaService>();

        //services.AddSingleton<DebugService>();
        services.AddSingleton<IPluginService, PluginService>();

        NodeImplementFabirc.RegisterAssembly(typeof(MarsHostRootLayoutRenderNodeImpl).Assembly);

        //services.AddSingleton<IWebFilesService, WebFilesReadFilesystemService>();

        //services.AddScoped<KeycloakService>();
        //services.AddScoped<MarsSSOClientService>();
        //services.AddScoped<MarsSSOOpenIDServerService>();
        services.AddSingleton<IDevAdminConnectionService, DevAdminConnectionService>();
        services.AddSingleton<IServiceCollection>(services);
        services.AddSingleton<IMarsSystemService, MarsSystemService>();

        return services;
    }

}
