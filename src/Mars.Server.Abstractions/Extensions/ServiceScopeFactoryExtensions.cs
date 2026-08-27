using Microsoft.Extensions.DependencyInjection;

namespace Mars.Host.Shared.Extensions;

public static class ServiceScopeFactoryExtensions
{
    public static async Task<TResult> ExecuteScopedAsync<TService, TResult>(
        this IServiceScopeFactory scopeFactory,
        Func<TService, Task<TResult>> action)
            where TService : notnull
    {
        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TService>();
        return await action(service);
    }

    public static async Task ExecuteScopedAsync<TService>(
        this IServiceScopeFactory scopeFactory,
        Func<TService, Task> action)
            where TService : notnull
    {
        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TService>();
        await action(service);
    }
}
