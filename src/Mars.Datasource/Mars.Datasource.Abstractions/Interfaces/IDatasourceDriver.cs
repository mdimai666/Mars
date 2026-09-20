using Mars.Datasource.Abstractions.Mappings;
using Mars.Datasource.Abstractions.Sql;
using Mars.Datasource.Contracts.Sql;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;

namespace Mars.Datasource.Abstractions.Interfaces;

public interface IDatasourceDriver
{
    /// <summary>Выполнить запрос с возвратом данных.</summary>
    public Task<QueryResultDto> Query(DatasourceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Выполнить запрос без возврата данных (INSERT/UPDATE/DELETE/DDL)
    /// и вернуть число затронутых строк.
    /// </summary>
    public Task<DatasourceModifyResult> NonQuery(string sql, IReadOnlyList<DatasourceParam>? parameters = null, CancellationToken cancellationToken = default);

    public Task<Dictionary<string, QTableColumn>> Columns(string tableName);
    public Task<List<QTableSchema>> Tables();
    public Task<QDatabaseStructure> DatabaseStructure();

    /// <summary>
    /// Определение вьюхи так, как его хранит движок; null — объекта нет или это не вьюха.
    /// </summary>
    public Task<string?> ViewDefinition(string schemaName, string tableName);

    /// <summary>Полезные запросы движка: кнопки в UI и их обработчик живут у провайдера, а не в сервисе.</summary>
    public IReadOnlyList<DatasourceActionDescriptor> Actions { get; }

    /// <summary>Выполнить действие из <see cref="Actions"/>.</summary>
    public Task<QueryResultDto> ExecuteAction(string actionId, CancellationToken cancellationToken = default);
}
