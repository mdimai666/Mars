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
    public Task<SqlQueryResultActionDto> SqlQuery(string slug, string sql);
    public Task<SqlNonQueryResultActionDto> SqlNonQuery(string slug, string sql);
    public Task<UserActionResult<string[][]>> ExecuteAction(DatasourceActionRequest action, CancellationToken cancellationToken);
    public IEnumerable<SelectDatasourceDto> ListSelectDatasource();

}
