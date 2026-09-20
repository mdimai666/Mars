using System.Data.Common;
using Mars.Datasource.Abstractions.Mappings;
using Mars.Datasource.Abstractions.Sql;
using Mars.Datasource.Contracts.Sql;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using MySqlConnector;

namespace Mars.Datasource.Providers.MySQL;

public class DatasourceMySQLDriver(DatasourceConfig config) : SqlDatasourceDriverBase(config)
{
    protected override DbConnection CreateConnection() => new MySqlConnection(Config.ConnectionString);

    protected override DbCommand CreateCommand(string sql, DbConnection connection)
        => new MySqlCommand(sql, (MySqlConnection)connection);

    /// <summary>Таблицы и вьюхи базы одним запросом.</summary>
    protected override string TablesSql => """
        SELECT t.TABLE_SCHEMA AS schema_name,
               t.TABLE_NAME AS table_name,
               '' AS table_owner,
               CASE WHEN t.TABLE_TYPE = 'VIEW' THEN 'view' ELSE 'table' END AS kind
        FROM information_schema.TABLES t
        WHERE t.TABLE_SCHEMA = @database
        ORDER BY t.TABLE_NAME
        """;

    /// <summary>Колонки всех таблиц одним запросом, PK — через TABLE_CONSTRAINTS.</summary>
    protected override string ColumnsSql => """
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
        WHERE c.TABLE_SCHEMA = @database
        """;

    protected override string ColumnsOrderBySql => " ORDER BY c.TABLE_NAME, c.ORDINAL_POSITION";

    /// <summary>Определение вьюхи из `information_schema.VIEWS`; null — объекта нет.</summary>
    protected override string ViewDefinitionSql => """
        SELECT VIEW_DEFINITION
        FROM information_schema.VIEWS
        WHERE TABLE_NAME = @tableName
          AND (@schemaName = '' OR TABLE_SCHEMA = @schemaName)
        """;
}
