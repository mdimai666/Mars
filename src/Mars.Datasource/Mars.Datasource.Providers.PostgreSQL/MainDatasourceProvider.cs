using Mars.Datasource.Abstractions.Interfaces;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Datasource.Providers.PostgreSQL;

/// <summary>
/// Подключение провайдера PostgreSQL. Ядро модуля провайдеров не знает — их приносит
/// корень композиции (см. <c>AddDatasource</c> в проекте Mars.Datasource).
/// </summary>
public static class MainDatasourceProvider
{
    public static IServiceCollection AddDatasourcePostgreSql(this IServiceCollection services)
    {
        services.AddSingleton<IDatasourceDriverFactory, PostgreSqlDatasourceDriverFactory>();
        services.AddSingleton<IDatasourceBackupDriver, DatasourcePostgreSQLBackupDriver>();

        return services;
    }
}

internal class PostgreSqlDatasourceDriverFactory : IDatasourceDriverFactory
{
    public string Driver => "psql";

    public string DisplayName => "PostgreSQL";

    public string DefaultConnectionString => "Host=127.0.0.1;Database=database;Username=postgres;Password=123456;Port=5432";

    public string HelpLink => "https://www.npgsql.org/doc/basic-usage.html";

    public IDatasourceDriver Create(DatasourceConfig config) => new DatasourcePostgreSQLDriver(config);
}
