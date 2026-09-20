using System.Data.Common;
using Mars.Datasource.Abstractions.Mappings;
using Mars.Datasource.Abstractions.Sql;
using Mars.Datasource.Contracts.Sql;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Microsoft.Data.SqlClient;

namespace Mars.Datasource.Providers.MsSQL;

public class DatasourceMsSQLDriver(DatasourceConfig config) : SqlDatasourceDriverBase(config)
{
    protected override DbConnection CreateConnection() => new SqlConnection(Config.ConnectionString);

    protected override DbCommand CreateCommand(string sql, DbConnection connection)
        => new SqlCommand(sql, (SqlConnection)connection);

    /// <summary>Таблицы и вьюхи базы одним запросом.</summary>
    protected override string TablesSql => """
        SELECT t.TABLE_SCHEMA AS schema_name,
               t.TABLE_NAME AS table_name,
               '' AS table_owner,
               CASE WHEN t.TABLE_TYPE = 'VIEW' THEN 'view' ELSE 'table' END AS kind
        FROM INFORMATION_SCHEMA.TABLES t
        WHERE t.TABLE_CATALOG = @database
        ORDER BY t.TABLE_SCHEMA, t.TABLE_NAME
        """;

    /// <summary>Колонки всех таблиц одним запросом, PK — через TABLE_CONSTRAINTS.</summary>
    protected override string ColumnsSql => """
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
        WHERE c.TABLE_CATALOG = @database
        """;

    protected override string ColumnsOrderBySql => " ORDER BY c.TABLE_SCHEMA, c.TABLE_NAME, c.ORDINAL_POSITION";

    /// <summary>Определение вьюхи из `sys.sql_modules`; null — объекта нет.</summary>
    protected override string ViewDefinitionSql => """
        SELECT m.definition
        FROM sys.sql_modules m
        JOIN sys.objects o ON o.object_id = m.object_id
        JOIN sys.schemas s ON s.schema_id = o.schema_id
        WHERE o.name = @tableName
          AND (@schemaName = '' OR s.name = @schemaName)
        """;
}
