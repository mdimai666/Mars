using System.Text.RegularExpressions;

namespace Mars.Datasource.Contracts.Sql;

/// <summary>
/// Признак «опасного» SQL: такой запрос выполняется только после подтверждения пользователя.
/// </summary>
public static class SqlSafety
{
    /// <summary>Операторы, которые сносят структуру или права.</summary>
    private static readonly string[] DestructiveKeywords = ["DROP", "TRUNCATE", "ALTER", "GRANT", "REVOKE"];

    /// <summary>Операторы изменения данных: без WHERE меняют всю таблицу.</summary>
    private static readonly string[] MassChangeKeywords = ["DELETE", "UPDATE"];

    private static readonly Regex WhereRegex = new(@"\bwhere\b", RegexOptions.IgnoreCase);

    public static bool IsDestructive(string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return false;

        var text = StripLeadingComments(sql).TrimStart();
        var keyword = FirstWord(text);

        if (DestructiveKeywords.Contains(keyword)) return true;

        if (MassChangeKeywords.Contains(keyword)) return !WhereRegex.IsMatch(text);

        return false;
    }

    /// <summary>Первое слово запроса (для сообщения пользователю) — в верхнем регистре.</summary>
    public static string FirstWord(string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return "";

        var text = StripLeadingComments(sql).TrimStart();
        var index = 0;

        while (index < text.Length && (char.IsLetter(text[index]) || text[index] == '_')) index++;

        return text[..index].ToUpperInvariant();
    }

    private static string StripLeadingComments(string sql)
    {
        var index = 0;

        while (index < sql.Length)
        {
            if (char.IsWhiteSpace(sql[index]))
            {
                index++;
                continue;
            }

            if (index + 1 < sql.Length && sql[index] == '-' && sql[index + 1] == '-')
            {
                var end = sql.IndexOf('\n', index);
                if (end < 0) return "";
                index = end + 1;
                continue;
            }

            if (index + 1 < sql.Length && sql[index] == '/' && sql[index + 1] == '*')
            {
                var end = sql.IndexOf("*/", index + 2, StringComparison.Ordinal);
                if (end < 0) return "";
                index = end + 2;
                continue;
            }

            break;
        }

        return sql[index..];
    }
}
