using Mars.AiChat.Host.Hubs;
using Mars.AiChat.Host.Services;
using Mars.AiChat.Host.Shared.Interfaces;
using Mars.AiChat.Host.Tools;
using Mars.AiChat.Host.Toolsets;
using Mars.AiChat.Shared.Options;
using Mars.AiChat.Shared.SignalR;
using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.Services;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Mars.AiChat.Host;

public static class MainAiChat
{
    public static IServiceCollection AddMarsAiChat(this IServiceCollection services)
    {
        services.AddSingleton<IAiChatClientFactory, AiChatClientFactory>();
        services.AddSingleton<IAiChatSessionStore, AiChatSessionStore>();
        services.AddSingleton<IAiChatRunCoordinator, AiChatRunCoordinator>();
        services.AddSingleton<AiChatPageBridge>();

        // Память агента — общая на инстанс (<data>/ai/memory): свой FileMemoryProvider
        // с постоянным working folder, встроенный изолирует папку на сессию
        services.AddSingleton(sp =>
        {
            var dataRoot = sp.GetRequiredKeyedService<IOptions<FileHostingInfo>>("data").Value.PhysicalPath.LocalPath;
            var store = new FileSystemAgentFileStore(Path.Combine(dataRoot, "ai", "memory"));
            return new FileMemoryProvider(store, _ => new FileMemoryState { WorkingFolder = "" }, null);
        });

        services.AddScoped<AiChatAgentService>();
        services.AddScoped<MarsSiteTools>();
        services.AddScoped<MarsOptionsTools>();
        services.AddScoped<MarsSystemTools>();
        services.AddScoped<MarsSqlTools>();
        services.AddScoped<MarsHttpTools>();

        // Тулсеты: новый домен инструментов = новый класс IAiToolset + эта строка
        services.AddScoped<IAiToolset, CoreToolset>();
        services.AddScoped<IAiToolset, ContentToolset>();
        services.AddScoped<IAiToolset, PageToolset>();
        services.AddScoped<IAiToolset, SqlToolset>();
        services.AddScoped<IAiToolset, FrontToolset>();

        return services;
    }

    public static WebApplication UseMarsAiChat(this WebApplication app)
    {
        var optionService = app.Services.GetRequiredService<IOptionService>();
        optionService.RegisterOption<AiChatOption>();

        app.MapHub<AiChatHub>(AiChatHubEvents.HubPath, options =>
        {
            options.Transports = HttpTransportType.WebSockets | HttpTransportType.LongPolling;
        });

        return app;
    }
}
