using System.Text;

namespace Mars.Cms.Abstractions.Utils;

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

    /// <summary>
    /// Зарезервированные ключевые слова C# (контекстные var/record/get и т.п. — допустимые имена свойств).
    /// Ключ метаполя становится именем свойства в runtime-компилируемых Mto-моделях —
    /// ключевое слово сломает компиляцию всех моделей (CS1519).
    /// </summary>
    static readonly HashSet<string> CSharpKeywords =
    [
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
        "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
        "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
        "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
        "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
        "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
        "void", "volatile", "while",
    ];

    public static bool IsCSharpKeyword(string? key)
        => key is not null && CSharpKeywords.Contains(key);
}
