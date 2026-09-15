using Mars.Datasource.Abstractions.Interfaces;
using Mars.Datasource.Contracts.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Datasource.Providers.MsSQL;

/// <summary>
/// Подключение провайдера MsSQL. Ядро модуля провайдеров не знает — их приносит
/// корень композиции (см. <c>AddDatasource</c> в проекте Mars.Datasource).
/// </summary>
public static class MainDatasourceProvider
{
    public static IServiceCollection AddDatasourceMsSql(this IServiceCollection services)
    {
        services.AddSingleton<IDatasourceDriverFactory, MsSqlDatasourceDriverFactory>();

        return services;
    }
}

internal class MsSqlDatasourceDriverFactory : IDatasourceDriverFactory
{
    public string Driver => "mssql";

    public string DefaultConnectionString => "Server=.\\SQLEXPRESS;Database=database;User ID=sa;Password=123456;Trusted_Connection=True;TrustServerCertificate=True";

    public string HelpLink => "https://learn.microsoft.com/en-us/ef/core/";

    public IDatasourceDriver Create(DatasourceConfig config) => new DatasourceMsSQLDriver(config);
}
