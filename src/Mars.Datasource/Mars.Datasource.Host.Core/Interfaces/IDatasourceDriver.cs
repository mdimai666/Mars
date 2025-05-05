namespace Mars.Datasource.Core.Interfaces;

public interface IDatasourceDriver
{
    public Task<Dictionary<string, QTableColumn>> Columns(string tableName);
    public Task<List<QTableSchema>> Tables();
    public Task<QDatabaseStructure> DatabaseStructure();
    public Task<SqlQueryResultActionDto> SqlQuery(string sql);
}
