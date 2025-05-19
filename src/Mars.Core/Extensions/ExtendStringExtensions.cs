using System.Collections;

namespace Mars.Core.Extensions;

public static class ExtendStringExtensions
{
    public static IEnumerable<T> AsEmptyIfNull<T>(this IEnumerable<T>? source)
    {
        return source ?? Enumerable.Empty<T>();
    }

    public static T[] AsEmptyIfNull<T>(this T[]? source)
    {
        if (source != null)
        {
            return source;
        }

        return Array.Empty<T>();
    }

    public static string AsEmptyIfNull(this string? source)
    {
        if (source != null)
        {
            return source;
        }

        return string.Empty;
    }

    public static T? AsNullIfEmpty<T>(this T? source) where T : IEnumerable
    {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        if (!((object)source is string value) || !string.IsNullOrEmpty(value))
        {
            if (source?.GetEnumerator().MoveNext() ?? false)
            {
                return source;
            }

            return default(T);
        }
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

        return default(T);
    }

    public static string? AsNullIfEmptyOrWhiteSpace(this string? source)
    {
        if (!string.IsNullOrWhiteSpace(source))
        {
            return source;
        }

        return null;
    }
}
