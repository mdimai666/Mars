using System.Diagnostics;
using System.Text.Json.Nodes;
using Mars.Datasource.Abstractions.Interfaces;
using Mars.Datasource.Abstractions.Models;
using Npgsql;
using Npgsql.Schema;

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

    public async Task<Dictionary<string, QTableColumn>> Columns(NpgsqlConnection conn, string tableName)
    {
        string sql = $"SELECT * FROM {QuoteIdentifier(tableName)}";

        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        var cols = reader.GetColumnSchema();

        Dictionary<string, QTableColumn> dict = [];

        foreach (var col in cols)
        {
            dict.Add(col.ColumnName, ConvertQTableColumn(col));
        }

        return dict;
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
        string sql = @" SELECT schemaname,tablename,tableowner,tablespace,hasindexes,hasrules,hastriggers,rowsecurity 
                        FROM pg_catalog.pg_tables 
                        WHERE schemaname = 'public'";

        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        List<QTableSchema> list = [];

        if (reader.HasRows)
        {
            while (reader.Read())
            {
                //Console.WriteLine(reader.GetString(0));
                var a = ConvertQTableSchema(reader);
                list.Add(a);
            }
        }
        reader.Close();

        return list;
    }

    public async Task<List<QTableSchema>> Tables()
    {
        await using var conn = new NpgsqlConnection(_config.ConnectionString);
        await conn.OpenAsync();

        var list = await Tables(conn);

        return list;
    }

    public async Task<QDatabaseStructure> DatabaseStructure()
    {
        await using var conn = new NpgsqlConnection(_config.ConnectionString);
        await conn.OpenAsync();

        QDatabaseStructure db = new()
        {
            DatabaseName = conn.Database
        };

        List<QTableSchema> list = await Tables(conn);

        foreach (var table in list)
        {
            var columns = await Columns(conn, table.TableName);

            QTable qTable = new()
            {
                TableName = table.TableName,
                TableSchema = table,
                Columns = columns
            };

            db.Tables.Add(qTable);
        }

        return db;
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
            QueryResultMapping.ApplyParameters(cmd, request.Parameters);
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

    public static QTableColumn ConvertQTableColumn(NpgsqlDbColumn column)
    {
        QTableColumn _this = new()
        {
            ColumnName = column.ColumnName,
            ColumnOrdinal = column.ColumnOrdinal ?? 0,
            ColumnSize = column.ColumnSize,
            IsAutoIncrement = column.IsAutoIncrement,
            IsKey = column.IsKey,
            IsLong = column.IsLong,
            IsUnique = column.IsUnique,
            DataType = column.DataType!,
            DataTypeName = column.DataTypeName!
        };
        return _this;

    }

    public static QTableSchema ConvertQTableSchema(NpgsqlDataReader reader)
    {
        QTableSchema _this = new()
        {
            SchemaName = reader.GetString(0),
            TableName = reader.GetString(1),
            TableOwner = reader.GetString(2)
        };
        //_this.TableSpace = reader.GetString(3);
        //_this.HasIndexes = reader.GetBoolean(4);
        //_this.HasRules = reader.GetBoolean(5);
        //_this.HasTriggers = reader.GetBoolean(6);
        //_this.RowSecurity = reader.GetBoolean(7);

        return _this;
    }
}
