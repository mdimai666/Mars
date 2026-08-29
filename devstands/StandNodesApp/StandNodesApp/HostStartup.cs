using Mars.Admin.Framework.Interfaces;
using Mars.Admin.Framework.Services;
using Mars.Contracts.Dto.Files;
using Mars.Identity.Abstractions.Interfaces;
using Mars.Nodes.Abstractions.Hubs;
using Mars.Server.Abstractions.Managers;
using Mars.Server.Abstractions.Services;
using Mars.TemplateEngine.Host;
using Mars.XActions.Abstractions.Managers;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StandNodesApp.Mocks;
using StandNodesApp.Services;
using MOptions = Microsoft.Extensions.Options.Options;

namespace StandNodesApp;

public static class HostStartup
{
    public static void HostConfigureServices(this WebApplicationBuilder builder)
    {
        IWebHostEnvironment wenv = builder.Environment;

        var dataDirHostingInfo = MOptions.Create(new FileHostingInfo()
        {
            Backend = null,
            PhysicalPath = new Uri(Path.Combine(wenv.ContentRootPath, "data"), UriKind.Absolute),
            RequestPath = ""
        });

        var dataFs = new FileStorage(dataDirHostingInfo);

        builder.Services.AddKeyedSingleton<IOptions<FileHostingInfo>>("data", dataDirHostingInfo);
        builder.Services.AddKeyedSingleton<IFileStorage>("data", dataFs);

        builder.Services.AddMarsSignalRConfiguration();
        builder.Services.AddSingleton<IEventManager, EventManagerMock>();
        builder.Services.AddSingleton<IActionManager, ActionManagerMock>();
        builder.Services.AddSingleton<IRequestContext, RequestContextMock>();

        builder.Services.AddSingleton(builder.Services);
        builder.Services.TryAddSingleton<ModelInfoService>();
        builder.Services.TryAddSingleton<IBlazorPagesService, BlazorPagesService>();
        builder.Services.AddSingleton<IDevAdminConnectionService, DevAdminConnectionService>();

        builder.Services.MarsAddTemplateEngines();

        builder.Services.AddSingleton<IDistributedCache, MemoryDistributedCache>();
        builder.Services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(150),
                LocalCacheExpiration = TimeSpan.FromMinutes(15)
            };
        });
    }

    public static void HostConfigure(this WebApplication app)
    {
        //app.UseStaticFiles();
    }

    public static IServiceCollection AddMarsSignalRConfiguration(this IServiceCollection services)
    {
        services
            .AddSignalR(hubOptions =>
            {
                hubOptions.EnableDetailedErrors = true;
                hubOptions.KeepAliveInterval = TimeSpan.FromMinutes(1);
            })
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNamingPolicy = null;
            });
        services.AddSingleton<BroadcastHub>();

        return services;
    }
}
