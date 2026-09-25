namespace Mars.Datasource.Contracts.Sql;

/// <summary>
/// Признак «записывающего» HTTP-запроса: такой запрос выполняется только после подтверждения
/// пользователя — аналог <see cref="SqlSafety"/> для rest-источника.
/// </summary>
public static class RestSafety
{
    /// <summary>Методы, которые ничего не меняют в источнике.</summary>
    static readonly string[] ReadMethods = ["GET", "HEAD", "OPTIONS"];

    public static bool IsWrite(string? method)
        => !string.IsNullOrWhiteSpace(method)
           && !ReadMethods.Contains(method.Trim().ToUpperInvariant());

    /// <summary>Метод первого запроса в тексте `.http`-документа (для сообщения пользователю).</summary>
    public static string FirstMethod(string? http)
    {
        if (string.IsNullOrWhiteSpace(http)) return "";

        foreach (var line in http.Replace("\r\n", "\n").Split('\n'))
        {
            var trimmed = line.Trim();

            if (trimmed.Length == 0) continue;
            if (trimmed.StartsWith('#') || trimmed.StartsWith("//", StringComparison.Ordinal)) continue;
            if (trimmed.StartsWith('@') || trimmed.StartsWith("###", StringComparison.Ordinal)) continue;

            var word = trimmed.Split(' ', 2)[0].ToUpperInvariant();

            return word.Length is > 0 and <= 10 ? word : "";
        }

        return "";
    }
}
