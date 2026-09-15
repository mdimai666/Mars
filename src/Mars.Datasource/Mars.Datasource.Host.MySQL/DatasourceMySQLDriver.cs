using System.Data.Common;
using System.Diagnostics;
using Mars.Datasource.Abstractions.Interfaces;
using Mars.Datasource.Abstractions.Models;
using MySqlConnector;

namespace Mars.Datasource.Host.MySQL;

public class DatasourceMySQLDriver : IDatasourceDriver
{
    private DatasourceConfig _config;
    string database;

    public DatasourceMySQLDriver(DatasourceConfig config)
    {
        _config = config;
        database = config.GetDatabaseName();
    }

    public string QuoteIdentifier(string name)
        => "`" + name.Replace("`", "``") + "`";

    /// <summary>Таблицы и вьюхи базы одним запросом.</summary>
    const string TablesSql = @"
        SELECT t.TABLE_SCHEMA AS schema_name,
               t.TABLE_NAME AS table_name,
               '' AS table_owner,
               CASE WHEN t.TABLE_TYPE = 'VIEW' THEN 'view' ELSE 'table' END AS kind
        FROM information_schema.TABLES t
        WHERE t.TABLE_SCHEMA = @database
        ORDER BY t.TABLE_NAME";

    /// <summary>Колонки всех таблиц одним запросом, PK — через TABLE_CONSTRAINTS.</summary>
    const string ColumnsSql = @"
        SELECT c.TABLE_SCHEMA AS schema_name,
               c.TABLE_NAME AS table_name,
               c.COLUMN_NAME AS column_name,
               c.ORDINAL_POSITION AS column_ordinal,
               c.DATA_TYPE AS data_type_name,
               c.CHARACTER_MAXIMUM_LENGTH AS column_size,
               CASE WHEN c.IS_NULLABLE = 'YES' THEN 1 ELSE 0 END AS is_nullable,
               CASE WHEN pk.COLUMN_NAME IS NULL THEN 0 ELSE 1 END AS is_key
        FROM information_schema.COLUMNS c
        LEFT JOIN (
            SELECT kcu.TABLE_SCHEMA, kcu.TABLE_NAME, kcu.COLUMN_NAME
            FROM information_schema.TABLE_CONSTRAINTS tc
            JOIN information_schema.KEY_COLUMN_USAGE kcu
              ON kcu.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
             AND kcu.TABLE_SCHEMA = tc.TABLE_SCHEMA
            WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
        ) pk ON pk.TABLE_SCHEMA = c.TABLE_SCHEMA
            AND pk.TABLE_NAME = c.TABLE_NAME
            AND pk.COLUMN_NAME = c.COLUMN_NAME
        WHERE c.TABLE_SCHEMA = @database";

    const string ColumnsOrderBySql = " ORDER BY c.TABLE_NAME, c.ORDINAL_POSITION";

    public async Task<Dictionary<string, QTableColumn>> Columns(MySqlConnection conn, string tableName)
    {
        var metas = await ReadColumnsAsync(conn, tableName);

        return metas.ToDictionary(c => c.ColumnName, QDatabaseStructureBuilder.ToColumn);
    }

    public async Task<Dictionary<string, QTableColumn>> Columns(string tableName)
    {
        await using var conn = new MySqlConnection(_config.ConnectionString);
        await conn.OpenAsync();

        var dict = await Columns(conn, tableName);

        return dict;
    }

    public async Task<List<QTableSchema>> Tables(MySqlConnection conn)
    {
        var metas = await ReadTablesAsync(conn);

        return metas.Select(QDatabaseStructureBuilder.ToSchema).ToList();
    }

    public async Task<List<QTableSchema>> Tables()
    {
        await using var conn = new MySqlConnection(_config.ConnectionString);
        await conn.OpenAsync();

        var list = await Tables(conn);

        return list;
    }

    /// <summary>Определение вьюхи из `information_schema.VIEWS`; null — объекта нет.</summary>
    const string ViewDefinitionSql = @"
        SELECT VIEW_DEFINITION
        FROM information_schema.VIEWS
        WHERE TABLE_NAME = @tableName
          AND (@schemaName = '' OR TABLE_SCHEMA = @schemaName)";

    public async Task<string?> ViewDefinition(string schemaName, string tableName)
    {
        await using var conn = new MySqlConnection(_config.ConnectionString);
        await conn.OpenAsync();

        await using var cmd = new MySqlCommand(ViewDefinitionSql, conn);
        cmd.Parameters.AddWithValue("@schemaName", schemaName ?? "");
        cmd.Parameters.AddWithValue("@tableName", tableName);

        return await cmd.ExecuteScalarAsync() as string;
    }

    public async Task<QDatabaseStructure> DatabaseStructure()
    {
        await using var conn = new MySqlConnection(_config.ConnectionString);
        await conn.OpenAsync();

        var tables = await ReadTablesAsync(conn);
        var columns = await ReadColumnsAsync(conn, null);

        return QDatabaseStructureBuilder.Assemble(conn.Database, tables, columns);
    }

    async Task<List<QTableMeta>> ReadTablesAsync(MySqlConnection conn)
    {
        await using var cmd = new MySqlCommand(TablesSql, conn);
        cmd.Parameters.AddWithValue("@database", database);

        await using var reader = await cmd.ExecuteReaderAsync();

        List<QTableMeta> list = [];

        while (await reader.ReadAsync())
        {
            list.Add(QDatabaseStructureBuilder.ReadTable(reader));
        }

        return list;
    }

    async Task<List<QColumnMeta>> ReadColumnsAsync(MySqlConnection conn, string? tableName)
    {
        string sql = tableName is null
            ? ColumnsSql + ColumnsOrderBySql
            : ColumnsSql + " AND c.TABLE_NAME = @tableName" + ColumnsOrderBySql;

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@database", database);
        if (tableName is not null)
        {
            cmd.Parameters.AddWithValue("@tableName", tableName);
        }

        await using var reader = await cmd.ExecuteReaderAsync();

        List<QColumnMeta> list = [];

        while (await reader.ReadAsync())
        {
            list.Add(QDatabaseStructureBuilder.ReadColumn(reader));
        }

        return list;
    }

    public async Task<QueryResultDto> Query(SqlRequest request, CancellationToken cancellationToken = default)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        QueryResultDto result = new()
        {
            DatabaseDriver = _config.Driver,
            Command = request.Sql,
        };

        try
        {
            await using var conn = new MySqlConnection(_config.ConnectionString);
            await conn.OpenAsync(cancellationToken);

            await using var cmd = new MySqlCommand(request.Sql, conn);
            QueryResultMapping.ApplyParameters(cmd, request.Parameters);
            if (request.TimeoutSec is int timeoutSec)
            {
                cmd.CommandTimeout = timeoutSec;
            }

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            var columns = await reader.GetColumnSchemaAsync(cancellationToken);
            result.Columns = columns.Select(c => QueryResultMapping.Column(c)).ToArray();

            (result.Rows, result.Truncated) = await QueryResultMapping.ReadRowsAsync(reader, request.MaxRows, cancellationToken);
            result.Ok = true;
            result.Message = "success";
        }
        catch (Exception ex)
        {
            result.Ok = false;
            result.Message = QueryResultMapping.Error(ex);
        }

        stopwatch.Stop();
        result.ElapsedMs = stopwatch.ElapsedMilliseconds;

        return result;
    }

    public async Task<SqlNonQueryResultActionDto> NonQuery(string sql, IReadOnlyList<SqlParam>? parameters = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conn = new MySqlConnection(_config.ConnectionString);
            await conn.OpenAsync(cancellationToken);

            await using var cmd = new MySqlCommand(sql, conn);
            QueryResultMapping.ApplyParameters(cmd, parameters);
            var rowsAffected = await cmd.ExecuteNonQueryAsync(cancellationToken);

            return new SqlNonQueryResultActionDto
            {
                Ok = true,
                Message = "success",
                DatabaseDriver = _config.Driver,
                RowsAffected = rowsAffected,
            };
        }
        catch (Exception ex)
        {
            return new SqlNonQueryResultActionDto
            {
                Ok = false,
                Message = QueryResultMapping.Error(ex),
                DatabaseDriver = _config.Driver,
            };
        }
    }

}
