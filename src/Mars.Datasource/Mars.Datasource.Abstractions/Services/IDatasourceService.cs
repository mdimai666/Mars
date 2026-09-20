using Mars.Contracts.Common;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;

namespace Mars.Datasource.Abstractions.Services;

/// <summary>
/// Запросы к источнику: каталог объектов, выполнение и документ запросов пользователя.
/// Контракт не знает типов источников — всё, что зависит от источника, приходит из его профиля
/// и от самого провайдера (<see cref="IDatasourceActionProvider"/>).
/// </summary>
public interface IDatasourceService
{
    /// <summary>Каталог объектов источника: общий вид дерева для любого типа источника.</summary>
    public Task<DatasourceCatalog> Catalog(string slug);

    /// <summary>Перечитать каталог источника, минуя кэш.</summary>
    public Task<DatasourceCatalog> RefreshCatalog(string slug);

    /// <summary>Выполнить запрос с возвратом данных.</summary>
    public Task<QueryResultDto> Query(string slug, DatasourceRequest request, CancellationToken cancellationToken = default);

    /// <summary>Выполнить запрос, который меняет источник (INSERT/UPDATE/DDL, POST/PUT/DELETE).</summary>
    public Task<DatasourceModifyResult> Modify(string slug, DatasourceRequest request, CancellationToken cancellationToken = default);

    /// <summary>Выполнить действие источника (утилиту) из <see cref="DatasourceCatalog.Actions"/>.</summary>
    public Task<UserActionResult<string[][]>> ExecuteAction(string slug, DatasourceActionRequest action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Текст документа источника: имя — из профиля провайдера (<c>requests.http</c> у rest).
    /// Пустая строка — документа ещё нет.
    /// </summary>
    public Task<string> Document(string slug, string name);

    /// <summary>Сохранить документ источника; каталог после этого перечитывается.</summary>
    public Task<UserActionResult> SaveDocument(string slug, string name, string content);
}
