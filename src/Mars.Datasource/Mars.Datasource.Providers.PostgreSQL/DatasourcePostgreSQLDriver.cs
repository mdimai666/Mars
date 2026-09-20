using System.Data.Common;
using Mars.Datasource.Abstractions.Mappings;
using Mars.Datasource.Abstractions.Sql;
using Mars.Datasource.Contracts.Sql;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Npgsql;
using Npgsql.Schema;
using NpgsqlTypes;

namespace Mars.Datasource.Providers.PostgreSQL;

public class DatasourcePostgreSQLDriver(DatasourceConfig config) : SqlDatasourceDriverBase(config)
{
    protected override DbConnection CreateConnection() => new NpgsqlConnection(Config.ConnectionString);

    protected override DbCommand CreateCommand(string sql, DbConnection connection)
        => new NpgsqlCommand(sql, (NpgsqlConnection)connection);

    /// <summary>Npgsql принимает имена параметров без «@».</summary>
    protected override string ParameterPrefix => "";

    /// <summary>Схемы отбираются внутри самого SQL, поэтому параметр базы не нужен.</summary>
    protected override bool UsesDatabaseParameter => false;

    protected override string ColumnsTableFilterSql => " AND c.relname = @tableName";

    /// <summary>
    /// Строковые параметры отправляем как unknown: иначе Npgsql помечает их text, и сравнение
    /// с uuid/date/jsonb-колонкой падает с «operator does not exist». Значения приходят из грида
    /// строками, поэтому тип должен выводить сам Postgres из контекста.
    /// </summary>
    protected override Action<DbParameter>? ConfigureParameter { get; } = parameter =>
    {
        if (parameter is NpgsqlParameter npgsql && npgsql.Value is string)
        {
            npgsql.NpgsqlDbType = NpgsqlDbType.Unknown;
        }
    };

    /// <summary>Схема результата Npgsql знает первичный ключ колонки — базовый DbColumn нет.</summary>
    protected override DatasourceField MapField(DbColumn column)
        => AdoResultReader.Field(column, column is NpgsqlDbColumn { IsKey: true });

    public override IReadOnlyList<DatasourceActionDescriptor> Actions => PostgreSqlUsefulQueries.Actions;

    public override Task<QueryResultDto> ExecuteAction(string actionId, CancellationToken cancellationToken = default)
        => PostgreSqlUsefulQueries.Sql(actionId) is string sql
            ? Query(new DatasourceRequest { Query = sql }, cancellationToken)
            : base.ExecuteAction(actionId, cancellationToken);

    /// <summary>Таблицы, вьюхи и матвьюхи всех пользовательских схем одним запросом.</summary>
    protected override string TablesSql => """
        SELECT n.nspname AS schema_name,
               c.relname AS table_name,
               pg_get_userbyid(c.relowner) AS table_owner,
               CASE c.relkind WHEN 'v' THEN 'view' WHEN 'm' THEN 'matview' ELSE 'table' END AS kind
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE c.relkind IN ('r', 'p', 'v', 'm', 'f')
          AND n.nspname NOT IN ('pg_catalog', 'information_schema')
          AND n.nspname NOT LIKE 'pg\_%'
        ORDER BY n.nspname, c.relname
        """;

    /// <summary>Колонки всех таблиц одним запросом: схемы/вьюхи/матвьюхи, PK через pg_index.</summary>
    protected override string ColumnsSql => """
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
          AND n.nspname NOT LIKE 'pg\_%'
        """;

    protected override string ColumnsOrderBySql => " ORDER BY n.nspname, c.relname, a.attnum";

    /// <summary>
    /// Определение вьюхи (`pg_get_viewdef`). Ищем по каталогу, а не через `::regclass`:
    /// имена со схемой и кавычками не требуют склейки строки.
    /// </summary>
    protected override string ViewDefinitionSql => """
        SELECT pg_get_viewdef(c.oid, true)
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE c.relname = @tableName
          AND (@schemaName = '' OR n.nspname = @schemaName)
        LIMIT 1
        """;
}
