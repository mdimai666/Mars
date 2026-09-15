using Mars.Contracts.Common;
using Mars.Datasource.Abstractions.Models;

namespace Mars.Datasource.Abstractions.Services;

public interface IDatasourceService
{
    public DatasourceConfig DefaultConfig { get; }

    public void InvalidateLocalDictCache(DatasourceOption opt);
    public Task<UserActionResult> TestConnection(ConnectionStringTestDto dto);
    public Task<Dictionary<string, QTableColumn>> Columns(string slug, string tableName);
    public Task<List<QTableSchema>> Tables(string slug);
    public Task<QDatabaseStructure> DatabaseStructure(string slug);

    /// <summary>Выполнить запрос с возвратом данных.</summary>
    public Task<QueryResultDto> Query(string slug, SqlRequest request, CancellationToken cancellationToken = default);

    /// <summary>Выполнить запрос без возврата данных (INSERT/UPDATE/DELETE/DDL).</summary>
    public Task<SqlNonQueryResultActionDto> NonQuery(string slug, string sql, IReadOnlyList<SqlParam>? parameters = null, CancellationToken cancellationToken = default);

    public Task<UserActionResult<string[][]>> ExecuteAction(string slug, DatasourceActionRequest action, CancellationToken cancellationToken);
    public IEnumerable<SelectDatasourceDto> ListSelectDatasource();

}
