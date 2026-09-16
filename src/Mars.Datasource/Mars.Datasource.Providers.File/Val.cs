using System.Globalization;
using System.Linq.Dynamic.Core.CustomTypeProviders;

namespace Mars.Datasource.Providers.File;

/// <summary>
/// Функции приведения значения для запросов к файлу. Строки файла — текст, поэтому сравнение
/// «как число» или «как дата» делается через них: <c>Val.Num(age) &gt; 30</c>,
/// <c>Val.Date(created) &gt; Val.Date("2020-01-01")</c>, <c>Val.Str(name).StartsWith("a")</c>.
/// Непарсимое и пустое значение даёт null — сравнение с ним просто не отбирает строку,
/// в отличие от <c>Convert.ToInt64</c>, который на пустой ячейке бросает FormatException.
/// Имена пишутся именно так: свои типы Dynamic LINQ разбирает с учётом регистра,
/// поэтому <c>val.num(age)</c> не найдётся.
/// </summary>
[DynamicLinqType]
public static class Val
{
    static readonly string[] DateFormats =
    [
        "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss",
        "dd.MM.yyyy", "dd.MM.yyyy HH:mm:ss",
        "dd/MM/yyyy", "MM/dd/yyyy",
    ];

    public static string Str(object? value)
        => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";

    public static long? Num(object? value)
        => long.TryParse(Str(value).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : null;

    public static double? Dec(object? value)
        => double.TryParse(Str(value).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : null;

    public static DateTime? Date(object? value)
    {
        var text = Str(value).Trim();

        if (text.Length == 0) return null;

        if (DateTime.TryParseExact(text, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact)) return exact;

        return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed : null;
    }

    public static bool? Flag(object? value)
    {
        var text = Str(value).Trim();

        if (text.Length == 0) return null;
        if (bool.TryParse(text, out var parsed)) return parsed;

        return text.ToLowerInvariant() switch
        {
            "1" or "yes" or "y" or "+" or "да" => true,
            "0" or "no" or "n" or "-" or "нет" => false,
            _ => null,
        };
    }

    public static Guid? Id(object? value)
        => Guid.TryParse(Str(value).Trim(), out var result) ? result : null;
}
