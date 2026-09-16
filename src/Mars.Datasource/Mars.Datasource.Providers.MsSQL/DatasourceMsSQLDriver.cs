using System.Data.Common;
using System.Diagnostics;
using Mars.Datasource.Abstractions.Interfaces;
using Mars.Datasource.Abstractions.Models;
using Mars.Datasource.Contracts.Models;
using Microsoft.Data.SqlClient;

namespace Mars.Datasource.Providers.MsSQL;

public class DatasourceMsSQLDriver : IDatasourceDriver
{
    private DatasourceConfig _config;
    string database;

    public DatasourceMsSQLDriver(DatasourceConfig config)
    {
        _config = config;
        database = config.GetDatabaseName();
    }

    public string QuoteIdentifier(string name)
        => "[" + name.Replace("]", "]]") + "]";

    /// <summary>Таблицы и вьюхи базы одним запросом.</summary>
    const string TablesSql = @"
        SELECT t.TABLE_SCHEMA AS schema_name,
               t.TABLE_NAME AS table_name,
               '' AS table_owner,
               CASE WHEN t.TABLE_TYPE = 'VIEW' THEN 'view' ELSE 'table' END AS kind
        FROM INFORMATION_SCHEMA.TABLES t
        WHERE t.TABLE_CATALOG = @database
        ORDER BY t.TABLE_SCHEMA, t.TABLE_NAME";

    /// <summary>Колонки всех таблиц одним запросом, PK — через TABLE_CONSTRAINTS.</summary>
    const string ColumnsSql = @"
        SELECT c.TABLE_SCHEMA AS schema_name,
               c.TABLE_NAME AS table_name,
               c.COLUMN_NAME AS column_name,
               c.ORDINAL_POSITION AS column_ordinal,
               c.DATA_TYPE AS data_type_name,
               c.CHARACTER_MAXIMUM_LENGTH AS column_size,
               CAST(CASE WHEN c.IS_NULLABLE = 'YES' THEN 1 ELSE 0 END AS BIT) AS is_nullable,
               CAST(CASE WHEN pk.COLUMN_NAME IS NULL THEN 0 ELSE 1 END AS BIT) AS is_key
        FROM INFORMATION_SCHEMA.COLUMNS c
        LEFT JOIN (
            SELECT tc.TABLE_CATALOG, tc.TABLE_SCHEMA, tc.TABLE_NAME, kcu.COLUMN_NAME
            FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
            JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
              ON kcu.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
             AND kcu.TABLE_SCHEMA = tc.TABLE_SCHEMA
             AND kcu.TABLE_CATALOG = tc.TABLE_CATALOG
            WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
        ) pk ON pk.TABLE_CATALOG = c.TABLE_CATALOG
            AND pk.TABLE_SCHEMA = c.TABLE_SCHEMA
            AND pk.TABLE_NAME = c.TABLE_NAME
            AND pk.COLUMN_NAME = c.COLUMN_NAME
        WHERE c.TABLE_CATALOG = @database";

    const string ColumnsOrderBySql = " ORDER BY c.TABLE_SCHEMA, c.TABLE_NAME, c.ORDINAL_POSITION";

    public async Task<Dictionary<string, QTableColumn>> Columns(SqlConnection conn, string tableName)
    {
        var metas = await ReadColumnsAsync(conn, tableName);

        return metas.ToDictionary(c => c.ColumnName, QDatabaseStructureBuilder.ToColumn);
    }

    public async Task<Dictionary<string, QTableColumn>> Columns(string tableName)
    {
        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.OpenAsync();

        var dict = await Columns(conn, tableName);

        return dict;
    }

    public async Task<QDatabaseStructure> DatabaseStructure()
    {
        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.OpenAsync();

        var tables = await ReadTablesAsync(conn);
        var columns = await ReadColumnsAsync(conn, null);

        return QDatabaseStructureBuilder.Assemble(conn.Database, tables, columns);
    }

    public async Task<QueryResultDto> Query(DatasourceRequest request, CancellationToken cancellationToken = default)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        QueryResultDto result = new()
        {
            DatabaseDriver = _config.Driver,
            Command = request.Query,
        };

        try
        {
            await using var conn = new SqlConnection(_config.ConnectionString);
            await conn.OpenAsync(cancellationToken);

            await using var cmd = new SqlCommand(request.Query, conn);
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

    public async Task<SqlNonQueryResultActionDto> NonQuery(string sql, IReadOnlyList<DatasourceParam>? parameters = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conn = new SqlConnection(_config.ConnectionString);
            await conn.OpenAsync(cancellationToken);

            await using var cmd = new SqlCommand(sql, conn);
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

    public async Task<List<QTableSchema>> Tables(SqlConnection conn)
    {
        var metas = await ReadTablesAsync(conn);

        return metas.Select(QDatabaseStructureBuilder.ToSchema).ToList();
    }

    public async Task<List<QTableSchema>> Tables()
    {
        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.OpenAsync();

        var list = await Tables(conn);

        return list;
    }

    /// <summary>Определение вьюхи из `sys.sql_modules`; null — объекта нет.</summary>
    const string ViewDefinitionSql = @"
        SELECT m.definition
        FROM sys.sql_modules m
        JOIN sys.objects o ON o.object_id = m.object_id
        JOIN sys.schemas s ON s.schema_id = o.schema_id
        WHERE o.name = @tableName
          AND (@schemaName = '' OR s.name = @schemaName)";

    public async Task<string?> ViewDefinition(string schemaName, string tableName)
    {
        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.OpenAsync();

        await using var cmd = new SqlCommand(ViewDefinitionSql, conn);
        cmd.Parameters.AddWithValue("@schemaName", schemaName ?? "");
        cmd.Parameters.AddWithValue("@tableName", tableName);

        return await cmd.ExecuteScalarAsync() as string;
    }

    async Task<List<QTableMeta>> ReadTablesAsync(SqlConnection conn)
    {
        await using var cmd = new SqlCommand(TablesSql, conn);
        cmd.Parameters.AddWithValue("@database", database);

        await using var reader = await cmd.ExecuteReaderAsync();

        List<QTableMeta> list = [];

        while (await reader.ReadAsync())
        {
            list.Add(QDatabaseStructureBuilder.ReadTable(reader));
        }

        return list;
    }

    async Task<List<QColumnMeta>> ReadColumnsAsync(SqlConnection conn, string? tableName)
    {
        string sql = tableName is null
            ? ColumnsSql + ColumnsOrderBySql
            : ColumnsSql + " AND c.TABLE_NAME = @tableName" + ColumnsOrderBySql;

        await using var cmd = new SqlCommand(sql, conn);
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

}
