using System.Reflection;
using Mars.Server.Abstractions.Managers;
using Mars.Server.Managers;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.XActions.Host;

public static class MainXActions
{
    public static IServiceCollection AddMarsXActionsHost(this IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());

        services.AddSingleton<IActionManager, XActionManager>();

        return services;
    }
}
