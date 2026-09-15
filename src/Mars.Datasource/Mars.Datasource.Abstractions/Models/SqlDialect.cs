namespace Mars.Datasource.Abstractions.Models;

/// <summary>
/// Диалект SQL: всё, чем источники различаются в запросах, которые Mars собирает сам
/// (DDL вьюх, лимит строк при просмотре объекта).
/// </summary>
public enum SqlDialect
{
    Postgres,
    MsSql,
    MySql,
}

/// <summary>Соответствие «источник Mars → диалект» и правила идентификаторов для собранных нами запросов.</summary>
public static class SqlDialectMapping
{
    /// <summary>Диалект по `DatasourceConfig.Driver` (`psql` | `mssql` | `mysql`).</summary>
    public static SqlDialect Dialect(string? driver) => driver?.Trim().ToLowerInvariant() switch
    {
        "mssql" => SqlDialect.MsSql,
        "mysql" => SqlDialect.MySql,
        _ => SqlDialect.Postgres,
    };

    public static string Quote(SqlDialect dialect, string name) => dialect switch
    {
        SqlDialect.MsSql => "[" + name.Replace("]", "]]") + "]",
        SqlDialect.MySql => "`" + name.Replace("`", "``") + "`",
        _ => "\"" + name.Replace("\"", "\"\"") + "\"",
    };

    /// <summary>Имя объекта для запроса: со схемой, если она задана.</summary>
    public static string Target(SqlDialect dialect, string? schemaName, string tableName)
    {
        var schema = schemaName?.Trim();

        return string.IsNullOrEmpty(schema)
            ? Quote(dialect, tableName)
            : $"{Quote(dialect, schema)}.{Quote(dialect, tableName)}";
    }
}
