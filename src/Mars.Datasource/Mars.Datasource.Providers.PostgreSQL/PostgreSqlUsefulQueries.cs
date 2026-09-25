using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;

namespace Mars.Datasource.Providers.PostgreSQL;

/// <summary>
/// Полезные запросы PostgreSQL: кнопки в UI и их SQL живут у движка, сервис источника про них не знает.
/// Подписи — те, что показывает страница утилит. Источник: https://habr.com/ru/articles/696274/
/// </summary>
public static class PostgreSqlUsefulQueries
{
    public const string SizePretty = "pg_size_pretty";
    public const string DatabaseSize = "pg_database_size";
    public const string NamespacesSizes = "pg_namespaces_sizes";
    public const string TotalRelationSize = "pg_total_relation_size";
    public const string ConnectionsCount = "connections_count";
    public const string QueryInRunning = "query_in_running";
    public const string CheckDbTimezone = "check_db_timezone";

    public static IReadOnlyList<DatasourceActionDescriptor> Actions { get; } =
    [
        new() { Id = SizePretty, Label = SizePretty, Description = "Размер табличных пространств" },
        new() { Id = DatabaseSize, Label = DatabaseSize, Description = "Размер баз данных" },
        new() { Id = NamespacesSizes, Label = NamespacesSizes, Description = "Размер схем в базе данных" },
        new() { Id = TotalRelationSize, Label = TotalRelationSize, Description = "Размер таблиц" },
        new() { Id = ConnectionsCount, Label = ConnectionsCount, Description = "Показывает количество открытых подключений" },
        new() { Id = QueryInRunning, Label = QueryInRunning, Description = "Показывает выполняющиеся запросы" },
        new() { Id = CheckDbTimezone, Label = CheckDbTimezone, Description = "Проверяет часовой пояс базы" },
    ];

    public static string? Sql(string actionId) => actionId switch
    {
        SizePretty => """
            SELECT spcname, pg_size_pretty(pg_tablespace_size(spcname))
                    FROM pg_tablespace
                    WHERE spcname<>'pg_global';
            """,
        DatabaseSize => """
            SELECT pg_database.datname,
                        pg_size_pretty(pg_database_size(pg_database.datname)) AS size
                    FROM pg_database
                    ORDER BY pg_database_size(pg_database.datname) DESC;
            """,
        NamespacesSizes => """
            SELECT A.schemaname,
                       pg_size_pretty (SUM(pg_relation_size(C.oid))) as table,
                       pg_size_pretty (SUM(pg_total_relation_size(C.oid)-pg_relation_size(C.oid))) as index,
                       pg_size_pretty (SUM(pg_total_relation_size(C.oid))) as table_index,
                       SUM(n_live_tup)
                    FROM pg_class C
                    LEFT JOIN pg_namespace N ON (N.oid = C .relnamespace)
                    INNER JOIN pg_stat_user_tables A ON C.relname = A.relname
                    WHERE nspname NOT IN ('pg_catalog', 'information_schema')
                    AND C .relkind <> 'i'
                    AND nspname !~ '^pg_toast'
                    GROUP BY A.schemaname;
            """,
        TotalRelationSize => """
            SELECT schemaname,
                        C.relname AS "relation",
                        pg_size_pretty (pg_relation_size(C.oid)) as table,
                        pg_size_pretty (pg_total_relation_size (C.oid)-pg_relation_size(C.oid)) as index,
                        pg_size_pretty (pg_total_relation_size (C.oid)) as table_index,
                        n_live_tup
                    FROM pg_class C
                    LEFT JOIN pg_namespace N ON (N.oid = C .relnamespace)
                    LEFT JOIN pg_stat_user_tables A ON C.relname = A.relname
                    WHERE nspname NOT IN ('pg_catalog', 'information_schema')
                    AND C.relkind <> 'i'
                    AND nspname !~ '^pg_toast'
                    ORDER BY pg_total_relation_size (C.oid) DESC
            """,
        ConnectionsCount => """
            SELECT COUNT(*) as connections,
                           backend_type
                    FROM pg_stat_activity
                    where state = 'active' OR state = 'idle'
                    GROUP BY backend_type
                    ORDER BY connections DESC;
            """,
        QueryInRunning => """
            SELECT pid, age(clock_timestamp(), query_start), usename, query, state
                    FROM pg_stat_activity
                    WHERE state != 'idle' AND query NOT ILIKE '%pg_stat_activity%'
                    ORDER BY query_start desc;
            """,
        CheckDbTimezone => """
            SELECT *
                            FROM pg_timezone_names
                            WHERE name = current_setting('TIMEZONE')
            """,
        _ => null,
    };
}
