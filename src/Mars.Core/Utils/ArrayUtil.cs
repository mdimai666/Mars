namespace Mars.Core.Utils;

public static class ArrayUtil
{
    public static T[] MoveItem<T>(this T[] items, int oldIndex, int newIndex)
    {
        ArgumentNullException.ThrowIfNull(items, nameof(items));
        if (oldIndex == newIndex) return items;

        var list = items.ToList();

        var itemToMove = list[oldIndex];

        var oldEl = list[oldIndex];
        list.RemoveAt(oldIndex);
        list.Insert(newIndex, oldEl);

        return list.ToArray();
    }
}
