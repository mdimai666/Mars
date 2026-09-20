namespace Mars.Datasource.Contracts.Sql;

/// <summary>
/// SQL просмотра объекта из дерева: `SELECT *` по первичному ключу с ограничением строк.
///
/// Лимит ставим в сам запрос, а не только в серверный `SqlRequest.MaxRows`: база тогда не отдаёт
/// лишние широкие строки, а с `ORDER BY` планировщик делает top-N сортировку вместо полной.
/// </summary>
public static class BrowseSqlBuilder
{
    /// <summary>Собирает запрос просмотра; <paramref name="limit"/> &lt;= 0 — без ограничения.</summary>
    public static string Build(
        SqlDialect dialect,
        string? schemaName,
        string tableName,
        IReadOnlyList<string> keyColumns,
        int limit)
    {
        var target = SqlDialectMapping.Target(dialect, schemaName, tableName);

        var orderBy = keyColumns.Count == 0
            ? ""
            : $" ORDER BY {string.Join(", ", keyColumns.Select(c => SqlDialectMapping.Quote(dialect, c)))}";

        // Лимит у MsSQL идёт перед списком колонок, у остальных — в конце запроса.
        var msSql = dialect == SqlDialect.MsSql;
        var select = msSql && limit > 0 ? $"SELECT TOP {limit} *" : "SELECT *";
        var limitBy = !msSql && limit > 0 ? $" LIMIT {limit}" : "";

        return $"{select} FROM {target}{orderBy}{limitBy}";
    }

    /// <summary>Запрос общего числа строк объекта — для «всего N» в шапке грида.</summary>
    public static string Count(SqlDialect dialect, string? schemaName, string tableName)
        => $"SELECT COUNT(*) FROM {SqlDialectMapping.Target(dialect, schemaName, tableName)}";
}
