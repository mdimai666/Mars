using Mars.WebApiClient.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebApiClient;

public static class MarsWebApiClientExtensions
{
    public static IServiceCollection AddMarsWebApiClient(this IServiceCollection services)
    {
        services.AddScoped<IMarsWebApiClient, MarsWebApiClient>();

        return services;
    }
}
