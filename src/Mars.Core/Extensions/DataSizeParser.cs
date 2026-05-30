using System.Globalization;
using System.Text.RegularExpressions;

namespace Mars.Core.Extensions;

public static class DataSizeParser
{
    public const long KB = 1024;
    public const long MB = KB * 1024;
    public const long GB = MB * 1024;
    public const long TB = GB * 1024;

    public static long ParseToBytes(string sizeString)
    {
        if (string.IsNullOrWhiteSpace(sizeString))
        {
            throw new ArgumentException("Строка размера не может быть пустой.", nameof(sizeString));
        }

        // Регулярное выражение отделяет числовую часть от суффикса (например, "10.5" и "mb")
        var match = Regex.Match(sizeString.Trim(), @"^([\d.,]+)\s*([a-zA-Z]{1,2})?$", RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            throw new FormatException($"Неверный формат размера данных: '{sizeString}'");
        }

        // Заменяем точку/запятую для универсальной поддержки локали при парсинге double
        string numberPart = match.Groups[1].Value.Replace(',', '.');
        if (!double.TryParse(numberPart, NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
        {
            throw new FormatException($"Не удалось распознать число: '{numberPart}'");
        }

        string unitPart = match.Groups[2].Value.ToLowerInvariant();

        double multiplier = unitPart switch
        {
            "b" or "" => 1,
            "kb" or "k" => KB,
            "mb" or "m" => MB,
            "gb" or "g" => GB,
            "tb" or "t" => TB,
            _ => throw new FormatException($"Неизвестная единица измерения: '{unitPart}'")
        };

        return (long)(value * multiplier);
    }
}
