namespace Mars.Core.Utils;

public class DiffList
{
    public static DiffListResult<TSource> FindDifferences<TSource>(
        IReadOnlyCollection<TSource> existList,
        IReadOnlyCollection<TSource> newList,
        IEqualityComparer<TSource>? comparer = null)
    {
        HashSet<TSource> existingItems = [.. existList];
        HashSet<TSource> newItems = [.. newList];

        var toRemove = existingItems.Except(newItems, comparer).ToArray();
        var toAdd = newItems.Except(existingItems, comparer).ToArray();

        return new DiffListResult<TSource>(toRemove, toAdd);
    }

    public static DiffListResult<TSource> FindDifferencesBy<TSource, TKey>(
        IReadOnlyCollection<TSource> existList,
        IReadOnlyCollection<TSource> newList,
        Func<TSource, TKey> keySelector
    ) where TKey : notnull
    {
        // Создаем словари для существующих и новых элементов
        Dictionary<TKey, TSource> existingItems = existList.ToDictionary(keySelector);
        Dictionary<TKey, TSource> newItems = newList.ToDictionary(keySelector);

        // Определяем элементы, которых больше нет в новом списке (нужно удалить)
        List<TSource> toRemove = existingItems.Keys
            .Except(newItems.Keys)
            .Select(key => existingItems[key])
            .ToList();

        // Определяем новые элементы, которых не было раньше (нужно добавить)
        List<TSource> toAdd = newItems.Keys
            .Except(existingItems.Keys)
            .Select(key => newItems[key])
            .ToList();

        return new DiffListResult<TSource>(toRemove, toAdd);
    }
}

public record DiffListResult<TSource>(
    IReadOnlyCollection<TSource> ToRemove,
    IReadOnlyCollection<TSource> ToAdd)
{
    public bool HasChanges => ToRemove.Count + ToAdd.Count > 0;
}
