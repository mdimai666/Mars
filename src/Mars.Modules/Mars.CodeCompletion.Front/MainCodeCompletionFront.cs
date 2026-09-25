using Mars.CodeCompletion.Front.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.CodeCompletion.Front;

public static class MainCodeCompletionFront
{
    public static IServiceCollection AddCodeCompletionFront(this IServiceCollection services)
    {
        services.AddSingleton<CodeCompletionRegistry>();
        services.AddSingleton<ICodeCompletionAttacher>(sp => sp.GetRequiredService<CodeCompletionRegistry>());
        return services;
    }
}
