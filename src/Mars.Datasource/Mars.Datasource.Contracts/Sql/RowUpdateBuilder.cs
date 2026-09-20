using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
namespace Mars.Datasource.Contracts.Sql;

/// <summary>Готовый план правки строки: SQL с параметрами (значения в текст не подставляются).</summary>
public class SqlUpdatePlan
{
    public string Sql { get; set; } = "";
    public List<DatasourceParam> Parameters { get; set; } = [];
}

/// <summary>
/// Сборка UPDATE одной строки по первичному ключу. Правка ячейки в UI не выполняется молча:
/// сначала показывается этот SQL, потом он исполняется.
/// </summary>
public static class RowUpdateBuilder
{
    /// <summary>
    /// Возвращает null, если менять нечего или в ключе не хватает значения —
    /// без первичного ключа правка строки невозможна.
    /// </summary>
    public static SqlUpdatePlan? Build(
        string? schemaName,
        string tableName,
        Func<string, string> quoteIdentifier,
        IReadOnlyList<string> keyColumns,
        IReadOnlyDictionary<string, string?> keyValues,
        IReadOnlyDictionary<string, string?> changes)
    {
        if (changes.Count == 0) return null;
        if (keyColumns.Count == 0) return null;

        List<DatasourceParam> parameters = [];
        List<string> sets = [];
        List<string> conditions = [];
        var index = 0;

        foreach (var change in changes)
        {
            index++;
            var name = $"p{index}";
            sets.Add($"{quoteIdentifier(change.Key)} = @{name}");
            parameters.Add(new DatasourceParam { Name = name, Value = change.Value });
        }

        foreach (var keyColumn in keyColumns)
        {
            if (!keyValues.TryGetValue(keyColumn, out var keyValue)) return null;

            index++;
            var name = $"p{index}";
            conditions.Add($"{quoteIdentifier(keyColumn)} = @{name}");
            parameters.Add(new DatasourceParam { Name = name, Value = keyValue });
        }

        var target = string.IsNullOrEmpty(schemaName)
            ? quoteIdentifier(tableName)
            : $"{quoteIdentifier(schemaName)}.{quoteIdentifier(tableName)}";

        return new SqlUpdatePlan
        {
            Sql = $"UPDATE {target} SET {string.Join(", ", sets)} WHERE {string.Join(" AND ", conditions)}",
            Parameters = parameters,
        };
    }
}
