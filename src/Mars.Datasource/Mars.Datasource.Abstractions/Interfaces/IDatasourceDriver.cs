using Mars.Datasource.Abstractions.Models;

namespace Mars.Datasource.Abstractions.Interfaces;

public interface IDatasourceDriver
{
    /// <summary>Квотирование идентификатора (таблицы/колонки) по правилам движка.</summary>
    public string QuoteIdentifier(string name);

    /// <summary>Выполнить запрос с возвратом данных.</summary>
    public Task<QueryResultDto> Query(SqlRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Выполнить запрос без возврата данных (INSERT/UPDATE/DELETE/DDL)
    /// и вернуть число затронутых строк.
    /// </summary>
    public Task<SqlNonQueryResultActionDto> NonQuery(string sql, IReadOnlyList<SqlParam>? parameters = null, CancellationToken cancellationToken = default);

    public Task<Dictionary<string, QTableColumn>> Columns(string tableName);
    public Task<List<QTableSchema>> Tables();
    public Task<QDatabaseStructure> DatabaseStructure();
}
