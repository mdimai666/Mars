using System.Diagnostics.CodeAnalysis;
using Mars.Contracts.Common;
using Mars.Core.Extensions;

namespace Mars.Data.Extensions;

public static class PaginationExtensions
{
    public static PagingResult<T> AsPagingResult<T>(this IEnumerable<T> source, IBasicListQuery query)
    {
        IReadOnlyCollection<T> items;

        if (!string.IsNullOrEmpty(query.Sort)) source = source.OrderBySortStringParam(query.Sort);

        if (query.Skip > 0)
        {
            items = source.Skip(query.Skip).Take(query.Take).ToList();
        }
        else
        {
            items = source.Take(query.Take).ToList();
        }

        var sourceCount = source.Count();

        return new PagingResult<T>(items, query, sourceCount > items.Count, sourceCount);
    }

    public static PagingResult<TResult> ToMap<T, TResult>(this PagingResult<T> source, [NotNull] Func<T, TResult> mapper)
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        ArgumentNullException.ThrowIfNull(mapper, nameof(mapper));

        return new PagingResult<TResult>(source.Items.Select(x => mapper(x)).ToList(), source.Page, source.PageSize, source.HasMoreData, source.TotalCount);
    }

    public static PagingResult<TResult> ToMap<T, TResult>(this PagingResult<T> source, [NotNull] Func<IEnumerable<T>, IReadOnlyCollection<TResult>> listMapper)
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        ArgumentNullException.ThrowIfNull(listMapper, nameof(listMapper));

        return new PagingResult<TResult>(listMapper(source.Items), source.Page, source.PageSize, source.HasMoreData, source.TotalCount);
    }
}
