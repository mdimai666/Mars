using Mars.CodeCompletion.Host.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.CodeCompletion.Host;

public static class MainCodeCompletion
{
    public static IServiceCollection AddMarsCodeCompletion(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CodeCompletionOptions>(configuration.GetSection(CodeCompletionOptions.SectionKey));

        services.AddSingleton<CodeCompletionWorkspaceManager>();
        services.AddSingleton<CompletionQueryService>();
        services.AddSingleton<HoverQueryService>();
        services.AddSingleton<SignatureHelpQueryService>();
        services.AddSingleton<DiagnosticsQueryService>();
        services.AddSingleton<SemanticTokensQueryService>();
        return services;
    }
}
