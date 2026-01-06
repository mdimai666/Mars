namespace Mars.Core.Extensions;

public static class StringExtensions
{
    public static bool IsNullOrEmpty(this string? source)
        => string.IsNullOrEmpty(source);

    public static string TrimSubstringStart(
        this string source,
        string substring,
        StringComparison comparison = StringComparison.Ordinal)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(substring))
            return source;

        return source.StartsWith(substring, comparison)
            ? source.Substring(substring.Length)
            : source;
    }

    public static string TrimSubstringEnd(
        this string source,
        string substring,
        StringComparison comparison = StringComparison.Ordinal)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(substring))
            return source;

        return source.EndsWith(substring, comparison)
            ? source.Substring(0, source.Length - substring.Length)
            : source;
    }

    public static string TrimSubstring(
        this string source,
        string substring,
        StringComparison comparison = StringComparison.Ordinal)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(substring))
            return source;

        if (source.StartsWith(substring, comparison))
            source = source.Substring(substring.Length);

        if (source.EndsWith(substring, comparison))
            source = source.Substring(0, source.Length - substring.Length);

        return source;
    }
}
