using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebSiteProcessor;

public static class MarsWebSiteProcessorMain
{
    public static IServiceCollection AddMarsWebSiteProcessor(this IServiceCollection services)
    {

        return services;
    }

    public static IApplicationBuilder UseMarsWebSiteProcessor(this WebApplication app)
    {

        return app;
    }

}
