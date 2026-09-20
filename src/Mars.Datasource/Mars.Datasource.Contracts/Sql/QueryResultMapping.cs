using System.Globalization;

namespace Mars.Datasource.Contracts.Sql;

/// <summary>
/// Значения источника текстом: одинаково для провайдеров (записать в результат) и для фронта
/// (показать в гриде), поэтому живёт в контрактах, а не в серверных абстракциях.
/// ADO-часть (поля из схемы результата, чтение строк, параметры) — в <c>AdoResultReader</c>.
/// </summary>
public static class QueryResultMapping
{
    /// <summary>
    /// Значение в строку. Даты/время — в инвариантном формате, чтобы отредактированное
    /// значение можно было вернуть в источник параметром без потери смысла.
    /// </summary>
    public static string? Format(object? value)
        => value switch
        {
            null or DBNull => null,
            string s => s,
            DateTime dt => dt.ToString("o", CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToString("o", CultureInfo.InvariantCulture),
            DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            TimeOnly t => t.ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture),
            TimeSpan ts => ts.ToString("c", CultureInfo.InvariantCulture),
            byte[] bytes => Convert.ToBase64String(bytes),
            bool b => b ? "true" : "false",
            System.Collections.IEnumerable items when items is not System.Collections.IDictionary => FormatArray(items),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString(),
        };

    /// <summary>
    /// Массив (в контракте бывает только у Postgres) — в литерале `{a,"b,c"}`: так его показывает psql,
    /// и такое значение можно вернуть в `UPDATE` параметром, не пересобирая. Словари (hstore) не трогаем.
    /// </summary>
    static string FormatArray(System.Collections.IEnumerable items)
    {
        List<string> parts = [];

        foreach (var item in items)
        {
            parts.Add(item switch
            {
                null or DBNull => "NULL",
                System.Collections.IEnumerable nested when nested is not string => FormatArray(nested),
                _ => QuoteArrayItem(Format(item) ?? ""),
            });
        }

        return "{" + string.Join(",", parts) + "}";
    }

    static string QuoteArrayItem(string value)
        => value.Length == 0 || value.Equals("NULL", StringComparison.OrdinalIgnoreCase)
            || value.IndexOfAny(['{', '}', ',', '"', '\\', ' ', '\t', '\n', '\r']) >= 0
                ? "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\""
                : value;

    /// <summary>
    /// Дата и время для показа: без секунд, долей и часового пояса. В самом значении они остаются —
    /// иначе правка ячейки вернула бы в источник усечённое время, а копирование теряло бы пояс.
    /// </summary>
    public static string? DisplayDateTime(string? value)
    {
        if (value is null) return null;

        // "2026-09-15T10:30:00.0000000+03:00" → "2026-09-15 10:30"
        if (value.Length >= 16 && value[4] == '-' && value[10] == 'T') return $"{value[..10]} {value[11..16]}";

        // "10:30:00.0000000" → "10:30"
        if (value.Length >= 8 && value[2] == ':' && value[5] == ':') return value[..5];

        return value;
    }

    /// <summary>Текст ошибки источника в одну строку: в UI он показывается целиком.</summary>
    public static string Error(Exception ex)
    {
        var message = (ex.GetBaseException().Message ?? ex.Message).ReplaceLineEndings(" ").Trim();
        return message.Length > 500 ? message[..500] + "…" : message;
    }
}
