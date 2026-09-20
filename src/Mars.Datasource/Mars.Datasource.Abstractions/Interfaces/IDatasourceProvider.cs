using Mars.Contracts.Common;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;

namespace Mars.Datasource.Abstractions.Interfaces;

/// <summary>
/// Провайдер источника: каталог объектов и выполнение запросов. Один контракт на все типы источников,
/// язык запроса определяет <see cref="DatasourceRequest.Language"/>, а поведение источника в UI —
/// <see cref="Profile"/>.
/// </summary>
public interface IDatasourceProvider
{
    /// <summary>Описание типа источника: возможности, язык, подсказки, поля настроек.</summary>
    DatasourceKindProfile Profile { get; }

    Task<DatasourceCatalog> Catalog(CancellationToken cancellationToken = default);

    Task<QueryResultDto> Query(DatasourceRequest request, CancellationToken cancellationToken = default);

    Task<DatasourceModifyResult> Modify(DatasourceRequest request, CancellationToken cancellationToken = default);
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

/// <summary>
/// Провайдер с действиями источника (утилитами): список уходит в каталог, выполнение — по идентификатору.
/// Так новый источник добавляет свои утилиты без правок сервиса и контроллера.
/// </summary>
public interface IDatasourceActionProvider
{
    IReadOnlyList<DatasourceActionDescriptor> Actions { get; }

    Task<UserActionResult<string[][]>> ExecuteAction(DatasourceActionRequest request, CancellationToken cancellationToken = default);
}
