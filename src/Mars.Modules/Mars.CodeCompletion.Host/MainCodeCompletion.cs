using Mars.CodeCompletion.Host.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.CodeCompletion.Host;

public static class MainCodeCompletion
{
    public static IServiceCollection AddMarsCodeCompletion(this IServiceCollection services)
    {
        services.AddSingleton<CodeCompletionWorkspaceManager>();
        services.AddSingleton<CompletionQueryService>();
        services.AddSingleton<HoverQueryService>();
        services.AddSingleton<SignatureHelpQueryService>();
        services.AddSingleton<DiagnosticsQueryService>();
        return services;
    }
}
