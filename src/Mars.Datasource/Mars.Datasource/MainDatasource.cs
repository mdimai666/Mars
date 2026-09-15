using Mars.Datasource.Front;
using Mars.Datasource.Host;
using Mars.Datasource.Providers.MsSQL;
using Mars.Datasource.Providers.MySQL;
using Mars.Datasource.Providers.PostgreSQL;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Datasource;

/// <summary>
/// Модуль источников данных целиком: ядро (<c>Mars.Datasource.Host</c>), WASM-фронт и провайдеры
/// SQL-движков. Единственная точка подключения для корня композиции: WebApp зовёт
/// <see cref="AddDatasource"/> и <see cref="UseDatasource"/>, а не хуки частей модуля.
/// </summary>
public static class MainDatasource
{
    public static IServiceCollection AddDatasource(this IServiceCollection services)
        => services.AddDatasourceHost()
                   .AddDatasourceWorkspace()
                   .AddDatasourcePostgreSql()
                   .AddDatasourceMsSql()
                   .AddDatasourceMySql();

    public static IApplicationBuilder UseDatasource(this IApplicationBuilder app)
    {
        app.UseDatasourceHost();
        app.ApplicationServices.UseDatasourceWorkspace();

        return app;
    }
}
