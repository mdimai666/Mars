using System.Text;

namespace Mars.Host.Shared.Utils;

/// <summary>
/// Нормализация и формат ключа мета-поля: [a-z_][a-z0-9_]*.
/// </summary>
public static class MetaFieldKeyNormalizer
{
    public const string FormatPattern = "^[a-z_][a-z0-9_]*$";

    public static string Normalize(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return "";

        var sb = new StringBuilder(key.Length);
        foreach (var c in key.Trim().ToLowerInvariant())
        {
            if (c is >= 'a' and <= 'z' or >= '0' and <= '9' or '_')
                sb.Append(c);
            else if (c is '-' or ' ' or '.')
                sb.Append('_');
            // остальные символы отбрасываются
        }

        var result = sb.ToString();
        if (result.Length > 0 && char.IsAsciiDigit(result[0]))
            result = '_' + result;

        return result;
    }

    public static bool IsValid(string? key)
        => !string.IsNullOrEmpty(key) && System.Text.RegularExpressions.Regex.IsMatch(key, FormatPattern);
}
