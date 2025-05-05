using System.Text.Json.Nodes;
using Mars.Datasource.Core;
using Mars.Datasource.Core.Interfaces;
using Npgsql;
using Npgsql.Schema;
using static Npgsql.PostgresTypes.PostgresCompositeType;
using static Npgsql.Replication.PgOutput.Messages.RelationMessage;

namespace Mars.Datasource.Host.PostgreSQL;

public class DatasourcePostgreSQLDriver : IDatasourceDriver
{
    private DatasourceConfig _config = default!;

    public DatasourcePostgreSQLDriver(DatasourceConfig config)
    {
        _config = config;
    }

    //public void aa()
    //{
    //    DbContextOptionsBuilder optionsBuilder = new DbContextOptionsBuilder<DbContext>();
    //    optionsBuilder.UseNpgsql(configs["psql"].ConnectionString);
    //    using DbContext db = new DbContext(optionsBuilder.Options);
    //}

    public async Task<Dictionary<string, QTableColumn>> Columns(NpgsqlConnection conn, string tableName)
    {
        string sql = $"SELECT * FROM \"{tableName}\"";//TODO: escape

        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        var cols = await reader.GetColumnSchemaAsync();

        Dictionary<string, QTableColumn> dict = new();

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

        List<QTableSchema> list = new();

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

        QDatabaseStructure db = new();
        db.DatabaseName = conn.Database;

        List<QTableSchema> list = await Tables(conn);

        foreach (var table in list)
        {
            var columns = await Columns(conn, table.TableName);

            QTable qTable = new QTable
            {
                TableName = table.TableName,
                TableSchema = table,
                Columns = columns
            };

            db.Tables.Add(qTable);
        }

        return db;
    }

    public async Task<SqlQueryResultActionDto> SqlQuery(string sql)
    {
        try
        {
            await using var conn = new NpgsqlConnection(_config.ConnectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            List<List<string>> rows = new();

            var columns = await reader.GetColumnSchemaAsync();

            List<string> _cols = new();

            foreach (var col in columns)
            {
                _cols.Add(col.ColumnName);
            }

            rows.Add(_cols);

            while (await reader.ReadAsync())
            {
                List<string> list = new();
                //Console.WriteLine(reader.GetString(0));
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string val = reader.GetValue(i).ToString()!;

                    list.Add(val);
                }
                rows.Add(list);
            }

            return Result("success", true, rows.Select(s => s.ToArray()).ToArray());

        }
        catch (Exception ex)
        {
            return Result(ex.Message);
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
        Dictionary<uint, string> oid_table_dict = new();
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

            var columns = await reader.GetColumnSchemaAsync();

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
            return ResultJson(ex.Message);
        }
        finally
        {
            conn.Close();
        }
    }


    SqlQueryResultActionDto Result(string message, bool ok = false, string[][]? data = null)
    {
        return new SqlQueryResultActionDto
        {
            Ok = ok,
            Message = message,
            Data = data,
            DatabaseDriver = _config.Driver
        };
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
        QTableColumn _this = new();
        _this.ColumnName = column.ColumnName;
        _this.ColumnOrdinal = column.ColumnOrdinal ?? 0;
        _this.ColumnSize = column.ColumnSize;
        _this.IsAutoIncrement = column.IsAutoIncrement;
        _this.IsKey = column.IsKey;
        _this.IsLong = column.IsLong;
        _this.IsUnique = column.IsUnique;
        _this.DataType = column.DataType!;
        _this.DataTypeName = column.DataTypeName;
        return _this;

    }

    public static QTableSchema ConvertQTableSchema(NpgsqlDataReader reader)
    {
        QTableSchema _this = new();
        _this.SchemaName = reader.GetString(0);
        _this.TableName = reader.GetString(1);
        _this.TableOwner = reader.GetString(2);
        //_this.TableSpace = reader.GetString(3);
        //_this.HasIndexes = reader.GetBoolean(4);
        //_this.HasRules = reader.GetBoolean(5);
        //_this.HasTriggers = reader.GetBoolean(6);
        //_this.RowSecurity = reader.GetBoolean(7);

        return _this;
    }
}
