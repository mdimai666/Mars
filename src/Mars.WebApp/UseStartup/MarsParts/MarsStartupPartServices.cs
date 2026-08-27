//using Mars.Areas.Identity;
using Mars.Admin.Framework.Interfaces;
using Mars.Admin.Framework.Services;
using Mars.Handlers;
using Mars.Server;
using Mars.Identity.Host.Models;
using Mars.Media.Abstractions.Services;
using Mars.Server.Abstractions.Services;
using Mars.SiteEngine.Abstractions.Services;
using Mars.Identity.Host.Models;
using Mars.Media.Abstractions.Services;
using Mars.Server.Abstractions.Services;
using Mars.SiteEngine.Abstractions.Services;
using Mars.SiteEngine.Abstractions.WebSite;
using Mars.MetaModelGenerator;
using Mars.Nodes;
using Mars.Nodes.Abstractions;
using Mars.QueryLang.Host;
using Mars.Services;
using Mars.Admin.Framework.Tools;
using Mars.SiteEngine.Endpoints;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mars.UseStartup.MarsParts;

internal static class MarsStartupPartServices
{
    public static IServiceCollection AddMarsHostServices(this IServiceCollection services, IWebHostEnvironment wenv)
    {
        //services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<UserEntity>>();

        //services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.JwtSectionKey));

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
        services.AddScoped<AdminFrontRenderHandler>();

        //services.AddSingleton<DebugService>();
        services.TryAddSingleton<ModelInfoService>();
        services.TryAddSingleton<IBlazorPagesService, BlazorPagesService>();

        services.AddSingleton<IServiceCollection>(services);

        return services;
    }

    public static IServiceProvider UseMarsHostServices(this IServiceProvider services)
    {
        var nodeImplementFactory = services.GetRequiredService<INodeImplementFactory>();
        nodeImplementFactory.RegisterAssembly(typeof(RenderPageNodeImpl).Assembly);

        return services;
    }

}
