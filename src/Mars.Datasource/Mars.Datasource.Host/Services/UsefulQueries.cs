namespace Mars.Datasource.Host.Services;

internal class UsefulQueries
{
    // https://habr.com/ru/articles/696274/

    /// <summary>
    /// Размер табличных пространств
    /// </summary>
    public string pg_size_pretty()
    {
        return @"SELECT spcname, pg_size_pretty(pg_tablespace_size(spcname)) 
        FROM pg_tablespace
        WHERE spcname<>'pg_global';";
    }

    /// <summary>
    /// Размер баз данных
    /// </summary>
    public string pg_database_size()
    {
        return @"SELECT pg_database.datname,
            pg_size_pretty(pg_database_size(pg_database.datname)) AS size
        FROM pg_database
        ORDER BY pg_database_size(pg_database.datname) DESC;";
    }

    /// <summary>
    /// Размер схем в базе данных
    /// </summary>
    public string pg_namespaces_sizes()
    {
        return @"SELECT A.schemaname,
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
        GROUP BY A.schemaname;";
    }

    /// <summary>
    /// Размер таблиц
    /// </summary>
    public string pg_total_relation_size()
    {
        return @"SELECT schemaname,
            C.relname AS ""relation"",
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
        ORDER BY pg_total_relation_size (C.oid) DESC";
    }

    /// <summary>
    /// Показывает количество открытых подключений
    /// </summary>
    public string connections_count()
    {
        return @"SELECT COUNT(*) as connections,
               backend_type
        FROM pg_stat_activity
        where state = 'active' OR state = 'idle'
        GROUP BY backend_type
        ORDER BY connections DESC;";
    }

    /// <summary>
    /// Показывает выполняющиеся запросы
    /// </summary>
    public string query_in_running()
    {
        return @"SELECT pid, age(clock_timestamp(), query_start), usename, query, state
        FROM pg_stat_activity
        WHERE state != 'idle' AND query NOT ILIKE '%pg_stat_activity%'
        ORDER BY query_start desc;";
    }

    public string check_db_timezone()
    {
        /**
        SELECT current_setting('TIMEZONE');
        SELECT * FROM pg_timezone_names;
        Asia/Yakutsk
        \l
        ALTER DATABASE diary2 SET timezone TO 'Asia/Yakutsk';
        ALTER DATABASE
        */
        return @"SELECT * 
                FROM pg_timezone_names
                WHERE name = current_setting('TIMEZONE')";
    }
}
