using AppFront.Main.OptionEditForms;
using Mars.AiChat.Front.OptionForms;
using Mars.AiChat.Front.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.AiChat.Front;

public static class MainAiChatFront
{
    public static IServiceCollection AddAiChatFront(this IServiceCollection services)
    {
        services.AddScoped<IAiChatAppService, AiChatAppService>();
        services.AddScoped<AiChatHubClient>();

        return services;
    }

    public static IServiceProvider UseAiChatFront(this IServiceProvider services)
    {
        services.GetRequiredService<IOptionsFormsLocator>().RegisterAssembly(typeof(AiChatOptionEditForm).Assembly);

        return services;
    }
}
