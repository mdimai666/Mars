using System.Reflection;
using Mars.XActions.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.XActions.Abstractions.Managers;

public static class XActionsServiceCollectionExtensions
{
    /// <summary>
    /// Сканирует сборки и регистрирует все найденные реализации <see cref="IAct"/> в DI (scoped).
    /// Хэндлеры «просто существуют»: командами они становятся только после императивной
    /// регистрации XAction через <see cref="IActionManager.Add"/>.
    /// </summary>
    public static IServiceCollection AddXActionHandlers(this IServiceCollection services, params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
            foreach (var actType in GetActTypes(assembly))
                services.AddScoped(actType);

        return services;
    }

    public static IEnumerable<Type> GetActTypes(Assembly assembly)
        => assembly.GetTypes()
                   .Where(t => typeof(IAct).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false });
}
