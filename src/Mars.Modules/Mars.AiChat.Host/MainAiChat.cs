using System.Text;
using Mars.AiChat.Abstractions.Interfaces;
using Mars.AiChat.Contracts.Options;
using Mars.AiChat.Contracts.SignalR;
using Mars.AiChat.Host.CommandLine;
using Mars.AiChat.Host.Hubs;
using Mars.AiChat.Host.Services;
using Mars.AiChat.Host.Tools;
using Mars.AiChat.Host.Toolsets;
using Mars.CommandLine.Abstractions;
using Mars.Contracts.Dto.Files;
using Mars.Options.Abstractions.Services;
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
        // windows-1251/koi8-r/cp866 для ReadMediaFile (повторная регистрация безвредна)
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

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

        services.AddSingleton<AiSkillCatalog>();

        services.AddScoped<AiChatAgentService>();
        services.AddScoped<MarsSiteTools>();
        services.AddScoped<MarsOptionsTools>();
        services.AddScoped<MarsSystemTools>();
        services.AddScoped<MarsSqlTools>();
        services.AddScoped<MarsHttpTools>();
        services.AddScoped<MarsSkillsTools>();

        // Тулсеты: новый домен инструментов = новый класс IAiToolset + эта строка
        services.AddScoped<IAiToolset, CoreToolset>();
        services.AddScoped<IAiToolset, ContentToolset>();
        services.AddScoped<IAiToolset, MediaToolset>();
        services.AddScoped<IAiToolset, PageToolset>();
        services.AddScoped<IAiToolset, SqlToolset>();
        services.AddScoped<IAiToolset, FrontToolset>();
        services.AddScoped<IAiToolset, SkillsToolset>();

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

        app.Services.GetService<ICommandLineApi>()?.Register<AiChatCli>();

        return app;
    }
}
