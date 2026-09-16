using Mars.Datasource.Abstractions.Interfaces;
using Mars.Datasource.Contracts.Models;

namespace Mars.Datasource.Abstractions.Models;

/// <summary>sql-источник как провайдер: общий контракт поверх существующего драйвера движка.</summary>
public class SqlDatasourceProvider : ISqlDatasourceProvider
{
    public SqlDatasourceProvider(IDatasourceDriver driver, DatasourceConfig config)
    {
        Driver = driver;
        Config = config;
    }

    public IDatasourceDriver Driver { get; }

    DatasourceConfig Config { get; }

    public DatasourceCapabilities Capabilities => CatalogMapping.SqlCapabilities;

    public async Task<DatasourceCatalog> Catalog(CancellationToken cancellationToken = default)
        => CatalogMapping.FromStructure(await Driver.DatabaseStructure(), Config);

    public Task<QueryResultDto> Query(DatasourceRequest request, CancellationToken cancellationToken = default)
        => Driver.Query(request, cancellationToken);

    public Task<SqlNonQueryResultActionDto> Modify(DatasourceRequest request, CancellationToken cancellationToken = default)
        => Driver.NonQuery(request.Query, request.Parameters, cancellationToken);
}

/// <summary>Обёртка фабрики sql-драйверов в фабрику провайдеров: движки регистрируются как раньше.</summary>
public class SqlDatasourceProviderFactory : IDatasourceProviderFactory
{
    readonly IDatasourceDriverFactory _driverFactory;

    public SqlDatasourceProviderFactory(IDatasourceDriverFactory driverFactory)
    {
        _driverFactory = driverFactory;
    }

    public string Kind => DatasourceKind.Sql;

    public string Driver => _driverFactory.Driver;

    public string DisplayName => _driverFactory.Driver;

    public string DefaultConnectionString => _driverFactory.DefaultConnectionString;

    public string HelpLink => _driverFactory.HelpLink;

    public IDatasourceProvider Create(DatasourceConfig config)
        => new SqlDatasourceProvider(_driverFactory.Create(config), config);
}
