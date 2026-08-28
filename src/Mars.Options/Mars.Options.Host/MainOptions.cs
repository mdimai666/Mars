using Mars.Options.Host.Services;
using Mars.Options.Services;
using Mars.SiteEngine.Abstractions.WebSite;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Options.Host;

public static class MainOptions
{
    public static IServiceCollection AddMarsOptions(this IServiceCollection services)
    {
        services.AddSingleton<IOptionService, OptionService>();
        services.AddSingleton<IFrontRequestHandler, MaintenanceFrontRequestHandler>();
        return services;
    }
}
