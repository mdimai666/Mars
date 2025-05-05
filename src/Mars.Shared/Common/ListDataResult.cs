namespace Mars.Shared.Common;

public class ListDataResult<T>
{
    private static readonly ListDataResult<T> _empty = new(Array.Empty<T>(), false, 0);
    public IReadOnlyCollection<T> Items { get; }
    public bool HasMoreData { get; }
    public int? TotalCount { get; }

    public ListDataResult(IReadOnlyCollection<T> items, bool hasMoreData, int? totalCount)
    {
        Items = items;
        HasMoreData = hasMoreData;
        TotalCount = totalCount;
    }

    public static ListDataResult<T> Empty() => _empty;

}
