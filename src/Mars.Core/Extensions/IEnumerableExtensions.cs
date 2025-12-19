namespace Mars.Core.Extensions;

public static class IEnumerableExtensions
{
    public static bool None<TSource>(this IEnumerable<TSource> source) => !source.Any();

}
