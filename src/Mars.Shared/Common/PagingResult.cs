namespace Mars.Shared.Common;

public class PagingResult<T>
{
    private static readonly PagingResult<T> _empty = new(Array.Empty<T>(), 1, BasicListQuery.DefaultPageSize, false, null);
    public IReadOnlyCollection<T> Items { get; init; } = default!;

    public int Page { get; init; }
    public int PageSize { get; init; }
    public bool HasMoreData { get; init; }

    public int? TotalCount { get; init; }
    public int? TotalPages { get; init; }

    public PagingResult(IReadOnlyCollection<T> items, int page, int pageSize, bool hasMoreData, int? totalCount = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1, nameof(page));
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1, nameof(pageSize));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, BasicListQuery.MaxPageSize, nameof(pageSize));

        Items = items;
        Page = page;
        PageSize = pageSize;
        HasMoreData = hasMoreData;
        TotalCount = totalCount;

        if (totalCount is not null)
        {
            var totalPages = (int)Math.Ceiling(totalCount.Value / (double)pageSize);

            TotalPages = totalPages;
        }
    }

    public PagingResult(IReadOnlyCollection<T> items, IBasicListQuery listQuery, bool hasMoreData, int? totalCount = null)
        : this(items, page: listQuery.ConvertSkipTakeToPage(), pageSize: listQuery.Take, hasMoreData: hasMoreData, totalCount: totalCount)
    {

    }

    public PagingResult()
    {
    }

    public static PagingResult<T> Empty() => _empty;

    public bool HasPrevious => Page > 1;

    public bool HasNext => HasMoreData || Page < TotalPages;

    public int ItemsCount => Items.Count;
}
