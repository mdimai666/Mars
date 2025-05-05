using System.Reflection;
using Mars.Host.Shared.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Host.Shared.Startup;

public interface IMarsAppLifetimeService
{
    /// <summary>
    /// Start after application is launch
    /// <para/>
    /// Please use <see cref="StartupOrderAttribute" /> for morte control
    /// </summary>
    /// <returns></returns>
    [StartupOrder(10)]
    public Task OnStartupAsync();

    static IReadOnlyCollection<IMarsAppLifetimeService> GetOrderedList(IServiceCollection serviceCollection, WebApplication app)
    {
        var ss = serviceCollection.FirstOrDefault(s => s.ServiceType == typeof(INodeService));

        var servicesTypes = serviceCollection.Where(s => typeof(IMarsAppLifetimeService).IsAssignableFrom(s.ImplementationType))
                .Where(s => s.Lifetime == ServiceLifetime.Singleton)
                .Select(s => s.ServiceType).ToList();

        var services = servicesTypes.Select(s => (IMarsAppLifetimeService)app.Services.GetRequiredService(s))
                                    .Select(s => new
                                    {
                                        Service = s,
                                        Order = s.GetType().GetMethod(nameof(OnStartupAsync))!.GetCustomAttribute<StartupOrderAttribute>()?.Order ?? StartupOrderAttribute.DEFAULT_ORDER
                                    })
                                    .OrderBy(s => s.Order)
                                    .Select(s => s.Service)
                                    .ToArray();

        return services;
    }

    public static async void UseAppLifetime(IServiceCollection serviceCollection, WebApplication app)
    {
        var services = GetOrderedList(serviceCollection, app);

        foreach (var service in services)
        {
            await service.OnStartupAsync();
        }
    }
}

/// <summary>
/// bigger last, default = 10
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class StartupOrderAttribute : Attribute
{
    public const int DEFAULT_ORDER = 10;

    public int Order { get; private set; }
    public StartupOrderAttribute(int order = DEFAULT_ORDER)
    {
        Order = order;
    }
}
