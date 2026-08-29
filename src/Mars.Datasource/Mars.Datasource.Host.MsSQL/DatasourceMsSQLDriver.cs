using System.Data.Common;
using Mars.Datasource.Abstractions.Interfaces;
using Microsoft.Data.SqlClient;

namespace Mars.Datasource.Host.MsSQL;

public class DatasourceMsSQLDriver : IDatasourceDriver
{
    private DatasourceConfig _config;
    string database;

    public DatasourceMsSQLDriver(DatasourceConfig config)
    {
        _config = config;
        database = config.GetDatabaseName();
    }

    public async Task<Dictionary<string, QTableColumn>> Columns(SqlConnection conn, string tableName)
    {
        string sql = $"SELECT * FROM \"{tableName}\"";

        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        var cols = await reader.GetColumnSchemaAsync();

        Dictionary<string, QTableColumn> dict = [];

        foreach (var col in cols)
        {
            dict.Add(col.ColumnName, ConvertQTableColumn(col));
        }

        return dict;
    }

    public async Task<Dictionary<string, QTableColumn>> Columns(string tableName)
    {
        using SqlConnection conn = new(_config.ConnectionString);
        conn.Open();

        var dict = await Columns(conn, tableName);

        return dict;
    }

    public async Task<QDatabaseStructure> DatabaseStructure()
    {
        await using var conn = new SqlConnection(_config.ConnectionString);
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

    public async Task<SqlQueryResultActionDto> SqlQuery(string sql)
    {
        try
        {
            await using var conn = new SqlConnection(_config.ConnectionString);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            List<List<string>> rows = [];

            var columns = await reader.GetColumnSchemaAsync();

            List<string> _cols = [];

            foreach (var col in columns)
            {
                _cols.Add(col.ColumnName);
            }

            rows.Add(_cols);

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    List<string> list = [];
                    //Console.WriteLine(reader.GetString(0));
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string val = reader.GetValue(i).ToString()!;

                        list.Add(val);
                    }
                    rows.Add(list);
                }
            }
            reader.Close();

            return Result("success", true, rows.Select(s => s.ToArray()).ToArray());

        }
        catch (Exception ex)
        {
            return Result(ex.Message);
        }
    }

    public async Task<SqlNonQueryResultActionDto> SqlNonQuery(string sql)
    {
        try
        {
            await using var conn = new SqlConnection(_config.ConnectionString);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(sql, conn);
            var rowsAffected = await cmd.ExecuteNonQueryAsync();

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
                Message = ex.Message,
                DatabaseDriver = _config.Driver,
            };
        }
    }

    public async Task<List<QTableSchema>> Tables(SqlConnection conn)
    {
        //string sql = @"SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE'";
        string sql = $@" SELECT /*TABLE_CATALOG as databasename,*/ TABLE_SCHEMA as schemaname, TABLE_NAME as tablename, '' as tableowner
                        FROM INFORMATION_SCHEMA.TABLES
                        WHERE TABLE_TYPE='BASE TABLE' AND TABLE_CATALOG = '{database}'";

        await using var cmd = new SqlCommand(sql, conn);
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
        await using var conn = new SqlConnection(_config.ConnectionString);
        await conn.OpenAsync();

        var list = await Tables(conn);

        return list;
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

    public static QTableColumn ConvertQTableColumn(DbColumn column)
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

    public static QTableSchema ConvertQTableSchema(SqlDataReader reader)
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
