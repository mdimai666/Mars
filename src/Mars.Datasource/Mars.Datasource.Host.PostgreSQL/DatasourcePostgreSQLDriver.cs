using System.Data.Common;
using System.Diagnostics;
using System.Text.Json.Nodes;
using Mars.Datasource.Abstractions.Interfaces;
using Mars.Datasource.Abstractions.Models;
using Npgsql;
using Npgsql.Schema;
using NpgsqlTypes;

namespace Mars.Datasource.Host.PostgreSQL;

public class DatasourcePostgreSQLDriver : IDatasourceDriver
{
    private DatasourceConfig _config = default!;

    public DatasourcePostgreSQLDriver(DatasourceConfig config)
    {
        _config = config;
    }

    public string QuoteIdentifier(string name)
        => "\"" + name.Replace("\"", "\"\"") + "\"";

    /// <summary>
    /// Таблицы, вьюхи и матвьюхи всех пользовательских схем одним запросом.
    /// </summary>
    const string TablesSql = @"
        SELECT n.nspname AS schema_name,
               c.relname AS table_name,
               pg_get_userbyid(c.relowner) AS table_owner,
               CASE c.relkind WHEN 'v' THEN 'view' WHEN 'm' THEN 'matview' ELSE 'table' END AS kind
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE c.relkind IN ('r', 'p', 'v', 'm', 'f')
          AND n.nspname NOT IN ('pg_catalog', 'information_schema')
          AND n.nspname NOT LIKE 'pg\_%'
        ORDER BY n.nspname, c.relname";

    /// <summary>
    /// Колонки всех таблиц одним запросом: схемы/вьюхи/матвьюхи, PK через pg_index.
    /// </summary>
    const string ColumnsSql = @"
        SELECT n.nspname AS schema_name,
               c.relname AS table_name,
               a.attname AS column_name,
               a.attnum AS column_ordinal,
               format_type(a.atttypid, a.atttypmod) AS data_type_name,
               NULL::int AS column_size,
               NOT a.attnotnull AS is_nullable,
               (pk.attnum IS NOT NULL) AS is_key
        FROM pg_attribute a
        JOIN pg_class c ON c.oid = a.attrelid
        JOIN pg_namespace n ON n.oid = c.relnamespace
        LEFT JOIN (
            SELECT i.indrelid AS relid, keys.attnum AS attnum
            FROM pg_index i
            CROSS JOIN LATERAL unnest(i.indkey::int2[]) AS keys(attnum)
            WHERE i.indisprimary
        ) pk ON pk.relid = c.oid AND pk.attnum = a.attnum
        WHERE a.attnum > 0
          AND NOT a.attisdropped
          AND c.relkind IN ('r', 'p', 'v', 'm', 'f')
          AND n.nspname NOT IN ('pg_catalog', 'information_schema')
          AND n.nspname NOT LIKE 'pg\_%'";

    const string ColumnsOrderBySql = " ORDER BY n.nspname, c.relname, a.attnum";

    /// <summary>
    /// Строковые параметры отправляем как unknown: иначе Npgsql помечает их text, и сравнение
    /// с uuid/date/jsonb-колонкой падает с «operator does not exist». Значения приходят из грида
    /// строками, поэтому тип должен выводить сам Postgres из контекста.
    /// </summary>
    static readonly Action<DbParameter> UntypedStrings = parameter =>
    {
        if (parameter is NpgsqlParameter npgsql && npgsql.Value is string)
        {
            npgsql.NpgsqlDbType = NpgsqlDbType.Unknown;
        }
    };

    public async Task<Dictionary<string, QTableColumn>> Columns(NpgsqlConnection conn, string tableName)
    {
        var metas = await ReadColumnsAsync(conn, tableName);

        return metas.ToDictionary(c => c.ColumnName, QDatabaseStructureBuilder.ToColumn);
    }

    public async Task<Dictionary<string, QTableColumn>> Columns(string tableName)
    {
        await using var conn = new NpgsqlConnection(_config.ConnectionString);
        await conn.OpenAsync();

        var dict = await Columns(conn, tableName);

        return dict;
    }

    public async Task<List<QTableSchema>> Tables(NpgsqlConnection conn)
    {
        var metas = await ReadTablesAsync(conn);

        return metas.Select(QDatabaseStructureBuilder.ToSchema).ToList();
    }

    public async Task<List<QTableSchema>> Tables()
    {
        await using var conn = new NpgsqlConnection(_config.ConnectionString);
        await conn.OpenAsync();

        var list = await Tables(conn);

        return list;
    }

    /// <summary>
    /// Определение вьюхи (`pg_get_viewdef`). Ищем по каталогу, а не через `::regclass`:
    /// имена со схемой и кавычками не требуют склейки строки.
    /// </summary>
    const string ViewDefinitionSql = @"
        SELECT pg_get_viewdef(c.oid, true)
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE c.relname = @tableName
          AND (@schemaName = '' OR n.nspname = @schemaName)
        LIMIT 1";

    public async Task<string?> ViewDefinition(string schemaName, string tableName)
    {
        await using var conn = new NpgsqlConnection(_config.ConnectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(ViewDefinitionSql, conn);
        cmd.Parameters.AddWithValue("schemaName", schemaName ?? "");
        cmd.Parameters.AddWithValue("tableName", tableName);

        return await cmd.ExecuteScalarAsync() as string;
    }

    public async Task<QDatabaseStructure> DatabaseStructure()
    {
        await using var conn = new NpgsqlConnection(_config.ConnectionString);
        await conn.OpenAsync();

        var tables = await ReadTablesAsync(conn);
        var columns = await ReadColumnsAsync(conn, null);

        return QDatabaseStructureBuilder.Assemble(conn.Database, tables, columns);
    }

    async Task<List<QTableMeta>> ReadTablesAsync(NpgsqlConnection conn)
    {
        await using var cmd = new NpgsqlCommand(TablesSql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        List<QTableMeta> list = [];

        while (await reader.ReadAsync())
        {
            list.Add(QDatabaseStructureBuilder.ReadTable(reader));
        }

        return list;
    }

    async Task<List<QColumnMeta>> ReadColumnsAsync(NpgsqlConnection conn, string? tableName)
    {
        string sql = tableName is null
            ? ColumnsSql + ColumnsOrderBySql
            : ColumnsSql + " AND c.relname = @tableName" + ColumnsOrderBySql;

        await using var cmd = new NpgsqlCommand(sql, conn);
        if (tableName is not null)
        {
            cmd.Parameters.AddWithValue("tableName", tableName);
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
            await using var conn = new NpgsqlConnection(_config.ConnectionString);
            await conn.OpenAsync(cancellationToken);

            await using var cmd = new NpgsqlCommand(request.Sql, conn);
            QueryResultMapping.ApplyParameters(cmd, request.Parameters, UntypedStrings);
            if (request.TimeoutSec is int timeoutSec)
            {
                cmd.CommandTimeout = timeoutSec;
            }

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            var columns = await reader.GetColumnSchemaAsync(cancellationToken);
            result.Columns = columns.Select(c => QueryResultMapping.Column(c, c.IsKey == true)).ToArray();

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
            await using var conn = new NpgsqlConnection(_config.ConnectionString);
            await conn.OpenAsync(cancellationToken);

            await using var cmd = new NpgsqlCommand(sql, conn);
            QueryResultMapping.ApplyParameters(cmd, parameters, UntypedStrings);
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

    Dictionary<uint, string> f_oid_table_dict(IEnumerable<uint> tablesOIDs)
    {
        using var conn = new NpgsqlConnection(_config.ConnectionString);
        conn.Open();
        //var tablesOIDs = columns.Select(s => s.TableOID).Distinct();
        string tablesIOdsQuery = $"SELECT oid,relname FROM pg_class WHERE oid IN ({string.Join(',', tablesOIDs)})";
        using var cmd_cols = new NpgsqlCommand(tablesIOdsQuery, conn);
        using var reader_cols = cmd_cols.ExecuteReader();
        Dictionary<uint, string> oid_table_dict = [];
        while (reader_cols.Read())
        {
            oid_table_dict.Add(uint.Parse(reader_cols.GetValue(0).ToString()!), reader_cols.GetString(1));
        }
        reader_cols.Close();
        conn.Close();

        return oid_table_dict;
    }

    public async Task<SqlQueryJsonResultActionDto> SqlQueryJson(string sql)
    {
        await using var conn = new NpgsqlConnection(_config.ConnectionString);
        await conn.OpenAsync();

        try
        {

            using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            var jArray = new JsonArray();

            var columns = reader.GetColumnSchema();

            //conn.Database.

            //-------------------------------------

            var tablesOIDs = columns.Select(s => s.TableOID).Distinct();
            var oid_table_dict = f_oid_table_dict(tablesOIDs);
            bool isMultipleTable = tablesOIDs.Count() > 0;

            List<string> _cols = new(columns.Count);

            foreach (var col in columns)
            {
                _cols.Add(col.ColumnName);
            }

            while (await reader.ReadAsync())
            {
                var jObject = new JsonObject();
                //Console.WriteLine(reader.GetString(0));
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var column = columns[i];
                    var tableName = oid_table_dict[column.TableOID]!;
                    var val = reader.GetValue(i);
                    //var val = reader.GetString(i);

                    if (isMultipleTable)
                    {
                        jObject.Add(tableName + '.' + column.ColumnName, JsonValue.Create(val));
                    }
                    else
                    {
                        jObject.Add(column.ColumnName, JsonValue.Create(val));
                    }
                }
                jArray.Add(jObject);
            }

            return ResultJson("success", true, jArray, columns.Select(s => s.ColumnName).ToArray());

        }
        catch (Exception ex)
        {
            return ResultJson(QueryResultMapping.Error(ex));
        }
        finally
        {
            conn.Close();
        }
    }

    SqlQueryJsonResultActionDto ResultJson(string message)
    {
        return ResultJson(message, false, null, null);
    }
    SqlQueryJsonResultActionDto ResultJson(string message, bool ok, JsonArray? data, string[]? fields)
    {
        return new SqlQueryJsonResultActionDto
        {
            Ok = ok,
            Message = message,
            Data = data,
            Fields = fields,
            DatabaseDriver = _config.Driver
        };
    }
}
