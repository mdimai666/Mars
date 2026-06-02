using System.Globalization;
using System.Text.RegularExpressions;

namespace Mars.Core.Features;

public static class TimeSpanParser
{
    private static readonly Regex Regex =
        new(@"^\s*(?<value>\d+(\.\d+)?)\s*(?<unit>ms|s|m|h|d)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static TimeSpan Parse(string? input)
    {
        if (!TryParse(input, out var result))
            throw new FormatException($"Invalid duration: '{input}'");

        return result;
    }

    public static TimeSpan? ParseOrNull(string? input)
    {
        if (!TryParse(input, out var result)) return null;

        return result;
    }

    public static bool TryParse(string? input, out TimeSpan result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        var match = Regex.Match(input);

        if (!match.Success)
            return false;

        var value = double.Parse(
            match.Groups["value"].Value,
            CultureInfo.InvariantCulture);

        var unit = match.Groups["unit"].Value.ToLowerInvariant();

        result = unit switch
        {
            "ms" => TimeSpan.FromMilliseconds(value),
            "s" => TimeSpan.FromSeconds(value),
            "m" => TimeSpan.FromMinutes(value),
            "h" => TimeSpan.FromHours(value),
            "d" => TimeSpan.FromDays(value),
            _ => default
        };

        return true;
    }

    public static string Format(
    TimeSpan value,
    IFormatProvider? formatProvider = null)
    {
        formatProvider ??= CultureInfo.InvariantCulture;

        if (value.TotalDays >= 1)
            return $"{value.TotalDays.ToString("0.##", formatProvider)}d";

        if (value.TotalHours >= 1)
            return $"{value.TotalHours.ToString("0.##", formatProvider)}h";

        if (value.TotalMinutes >= 1)
            return $"{value.TotalMinutes.ToString("0.##", formatProvider)}m";

        if (value.TotalSeconds >= 1)
            return $"{value.TotalSeconds.ToString("0.##", formatProvider)}s";

        return $"{value.TotalMilliseconds.ToString("0.##", formatProvider)}ms";
    }
}
