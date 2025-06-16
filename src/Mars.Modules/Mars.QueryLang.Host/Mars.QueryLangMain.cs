using Mars.Host.Shared.QueryLang.Services;
using Mars.QueryLang.Host.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.QueryLang.Host;

public static class MarsQueryLangMain
{
    public static IServiceCollection AddMarsQueryLang(this IServiceCollection services)
    {

        services.AddScoped<IQueryLangProcessing, QueryLangProcessing>();
        services.AddScoped<IQueryLangLinqDatabaseQueryHandler, QueryLangLinqDatabaseQueryHandler>();

        return services;
    }

}
