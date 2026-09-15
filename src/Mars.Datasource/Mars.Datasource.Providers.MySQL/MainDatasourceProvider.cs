using Mars.Datasource.Abstractions.Interfaces;
using Mars.Datasource.Contracts.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Datasource.Providers.MySQL;

/// <summary>
/// Подключение провайдера MySQL. Ядро модуля провайдеров не знает — их приносит
/// корень композиции (см. <c>AddDatasource</c> в проекте Mars.Datasource).
/// </summary>
public static class MainDatasourceProvider
{
    public static IServiceCollection AddDatasourceMySql(this IServiceCollection services)
    {
        services.AddSingleton<IDatasourceDriverFactory, MySqlDatasourceDriverFactory>();

        return services;
    }
}

internal class MySqlDatasourceDriverFactory : IDatasourceDriverFactory
{
    public string Driver => "mysql";

    public string DefaultConnectionString => "server=127.0.0.1;database=database;uid=user;pwd=123456;port=3306;Connection Timeout=2;persistsecurityinfo=True;SslMode=none;AllowZeroDateTime=True";

    public string HelpLink => "https://dev.mysql.com/doc/connector-net/en/connector-net-connections-string.html";

    public IDatasourceDriver Create(DatasourceConfig config) => new DatasourceMySQLDriver(config);
}
