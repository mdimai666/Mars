using Mars.Contracts.Common;
using Mars.Datasource.Abstractions.Interfaces;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;

namespace Mars.Datasource.Abstractions.Sql;

/// <summary>sql-источник как провайдер: общий контракт поверх существующего драйвера движка.</summary>
public class SqlDatasourceProvider : ISqlDatasourceProvider, IDatasourceActionProvider
{
    public SqlDatasourceProvider(IDatasourceDriver driver, DatasourceConfig config, DatasourceKindProfile profile)
    {
        Driver = driver;
        Config = config;
        Profile = profile;
    }

    public IDatasourceDriver Driver { get; }

    DatasourceConfig Config { get; }

    public DatasourceKindProfile Profile { get; }

    public IReadOnlyList<DatasourceActionDescriptor> Actions => Driver.Actions;

    public async Task<DatasourceCatalog> Catalog(CancellationToken cancellationToken = default)
    {
        var catalog = CatalogMapping.FromStructure(await Driver.DatabaseStructure(), Config, Profile);

        catalog.Actions = [.. Driver.Actions];

        return catalog;
    }

    public Task<QueryResultDto> Query(DatasourceRequest request, CancellationToken cancellationToken = default)
        => Driver.Query(request, cancellationToken);

    public Task<DatasourceModifyResult> Modify(DatasourceRequest request, CancellationToken cancellationToken = default)
        => Driver.NonQuery(request.Query, request.Parameters, cancellationToken);

    public async Task<UserActionResult<string[][]>> ExecuteAction(DatasourceActionRequest request, CancellationToken cancellationToken = default)
    {
        var result = await Driver.ExecuteAction(request.ActionId, cancellationToken);

        return new UserActionResult<string[][]>
        {
            Ok = result.Ok,
            Message = result.Message,
            Data = result.Data ?? [],
        };
    }
}

/// <summary>Обёртка фабрики sql-драйверов в фабрику провайдеров: движки регистрируются как раньше.</summary>
public class SqlDatasourceProviderFactory : IDatasourceProviderFactory
{
    readonly IDatasourceDriverFactory _driverFactory;

    public SqlDatasourceProviderFactory(IDatasourceDriverFactory driverFactory)
    {
        _driverFactory = driverFactory;
        Profile = SqlDatasourceProfile.Create(driverFactory);
    }

    public DatasourceKindProfile Profile { get; }

    public IDatasourceProvider Create(DatasourceConfig config)
        => new SqlDatasourceProvider(_driverFactory.Create(config), config, Profile);
}
