using System.Diagnostics.CodeAnalysis;
using Mars.Contracts.Common;
using Mars.Core.Extensions;

namespace Mars.Data.Extensions;

public static class ListDataExtensions
{
    public static ListDataResult<T> AsListDataResult<T>(this IEnumerable<T> source, IBasicListQuery query)
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

        return new ListDataResult<T>(items, sourceCount > items.Count, sourceCount);
    }

    public static ListDataResult<TResult> ToMap<T, TResult>(this ListDataResult<T> source, [NotNull] Func<T, TResult> mapper)
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        ArgumentNullException.ThrowIfNull(mapper, nameof(mapper));

        return new ListDataResult<TResult>(source.Items.Select(mapper).ToList(), source.HasMoreData, source.TotalCount);
    }

    public static ListDataResult<TResult> ToMap<T, TResult>(this ListDataResult<T> source, [NotNull] Func<IEnumerable<T>, IReadOnlyCollection<TResult>> listMapper)
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        ArgumentNullException.ThrowIfNull(listMapper, nameof(listMapper));

        return new ListDataResult<TResult>(listMapper(source.Items), source.HasMoreData, source.TotalCount);
    }
}
