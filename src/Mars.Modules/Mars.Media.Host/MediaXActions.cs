using Mars.Media.Host.XActions;
using Mars.XActions.Abstractions.Managers;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Media.Host;

public static class MediaXActions
{
    public static IServiceCollection AddMediaXActions(this IServiceCollection services)
    {
        services.AddXActionHandlers(typeof(ScanMediaFilesAct).Assembly);

        return services;
    }
}
