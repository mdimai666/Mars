using Mars.AiChat.Host.Hubs;
using Mars.AiChat.Host.Services;
using Mars.AiChat.Host.Shared.Interfaces;
using Mars.AiChat.Host.Tools;
using Mars.AiChat.Shared.Options;
using Mars.AiChat.Shared.SignalR;
using Mars.Host.Shared.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.AiChat.Host;

public static class MainAiChat
{
    public static IServiceCollection AddMarsAiChat(this IServiceCollection services)
    {
        services.AddSingleton<IAiChatClientFactory, AiChatClientFactory>();
        services.AddSingleton<IAiChatSessionStore, AiChatSessionStore>();
        services.AddSingleton<IAiChatRunCoordinator, AiChatRunCoordinator>();
        services.AddScoped<AiChatAgentService>();
        services.AddScoped<MarsSiteTools>();
        services.AddScoped<MarsOptionsTools>();
        services.AddScoped<MarsSystemTools>();

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
