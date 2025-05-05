using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Host.Data;

public static class Main
{
    public static IServiceCollection AddMarsHostData(this IServiceCollection services, string connectionString)
    {
        return services;
    }

    public static IApplicationBuilder UseMarsHostData(this WebApplication app)
    {
        return app;
    }
}
