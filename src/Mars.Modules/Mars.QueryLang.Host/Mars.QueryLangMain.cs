using Mars.QueryLang.Host.Helpers;
using Mars.QueryLang.Host.Services;
using Mars.QueryLang.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.QueryLang.Host;

public static class MarsQueryLangMain
{
    public static IServiceCollection AddMarsQueryLang(this IServiceCollection services)
    {

        services.AddScoped<IQueryLangProcessing, QueryLangProcessing>();
        services.AddScoped<IQueryLangLinqDatabaseQueryHandler, QueryLangLinqDatabaseQueryHandler>();
        services.AddSingleton<IQueryLangHelperAvailableMethodsProvider, QueryLangHelperAvailableMethodsProvider>();

        return services;
    }

}
