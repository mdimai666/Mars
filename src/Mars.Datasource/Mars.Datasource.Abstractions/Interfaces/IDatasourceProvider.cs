using Mars.Datasource.Contracts.Models;

namespace Mars.Datasource.Abstractions.Interfaces;

/// <summary>
/// Провайдер источника: каталог объектов и выполнение запросов. Один контракт на все типы источников,
/// язык запроса определяет <see cref="DatasourceRequest.Language"/>.
/// </summary>
public interface IDatasourceProvider
{
    DatasourceCapabilities Capabilities { get; }

    Task<DatasourceCatalog> Catalog(CancellationToken cancellationToken = default);

    Task<QueryResultDto> Query(DatasourceRequest request, CancellationToken cancellationToken = default);

    Task<SqlNonQueryResultActionDto> Modify(DatasourceRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// sql-провайдер: помимо общего контракта отдаёт драйвер движка для операций,
/// которых нет у других источников (схема базы, определение вьюхи, backup).
/// </summary>
public interface ISqlDatasourceProvider : IDatasourceProvider
{
    IDatasourceDriver Driver { get; }
}

/// <summary>
/// Провайдер, чей каталог собирается из внешнего описания API (discovery).
/// <see cref="IDatasourceProvider.Catalog"/> у него читает сохранённый каталог, а этот метод
/// перечитывает описание заново — его зовёт «обновить» в дереве объектов.
/// </summary>
public interface IDatasourceDiscoverableProvider : IDatasourceProvider
{
    Task<DatasourceCatalog> Discover(CancellationToken cancellationToken = default);
}
