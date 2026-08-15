using Mars.PxBlocks.Host.Services;
using Mars.PxBlocks.Host.Shared.Services;
using Mars.PxBlocks.Shared.Definitions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.PxBlocks.Host;

/// <summary>
/// Подключение серверного исполнения PxBlocks — по образцу MainMarsNodes:
/// AddPxBlocks регистрирует сервисы, UsePxBlocks — ядерные определения блоков;
/// доменные сборки регистрирует хост (catalog.RegisterAssembly).
/// </summary>
public static class MainPxBlocks
{
    public static IServiceCollection AddPxBlocks(this IServiceCollection services)
    {
        services.AddSingleton<IPxBlockCatalog, PxBlockCatalog>();
        services.AddSingleton<IPxBlocksBroadcaster, PxBlocksBroadcaster>();
        services.AddSingleton<IPxRunManager, PxRunManager>();
        services.AddSingleton<IPxEditorContextRegistry, PxEditorContextRegistry>();

        // JSON-протокол SignalR в конвенции Mars (StandNodesApp.AddMarsSignalRConfiguration):
        // имена методов/свойств как объявлены, без camelCase.
        services.AddSignalR()
            .AddJsonProtocol(options => options.PayloadSerializerOptions.PropertyNamingPolicy = null);

        return services;
    }

    public static IApplicationBuilder UsePxBlocks(this WebApplication app)
    {
        var catalog = app.Services.GetRequiredService<IPxBlockCatalog>();

        // Ядерные событийные блоки Start/Loop — определения доступны всегда.
        catalog.RegisterSet(new PxEventBlocks());

        return app;
    }
}
