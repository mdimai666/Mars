namespace Mars.Datasource.Core.Interfaces;

public interface IDatasourceDriver
{
    public Task<Dictionary<string, QTableColumn>> Columns(string tableName);
    public Task<List<QTableSchema>> Tables();
    public Task<QDatabaseStructure> DatabaseStructure();
    public Task<SqlQueryResultActionDto> SqlQuery(string sql);

    /// <summary>
    /// Выполнить запрос без возврата данных (INSERT/UPDATE/DELETE/DDL)
    /// и вернуть число затронутых строк.
    /// </summary>
    public Task<SqlNonQueryResultActionDto> SqlNonQuery(string sql)
        => Task.FromResult(new SqlNonQueryResultActionDto
        {
            Ok = false,
            Message = "Driver does not support SqlNonQuery",
        });
}
