using System.Diagnostics.CodeAnalysis;
using Mars.Core.Extensions;
using Mars.Shared.Common;
using Mars.Shared.Models.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Shared.Dto.Common;

public static class ListDataExtensions
{
    public static async Task<ListDataResult<T>> ToListDataResult<T>(this IQueryable<T> source, IBasicListQuery query, CancellationToken cancellationToken)
    {
        var sort = string.IsNullOrEmpty(query.Sort) ? $"-{nameof(IBasicEntity.CreatedAt)}" : query.Sort;

        var items = await source.OrderBySortStringParam(sort).ApplyPaging(query).ToListAsync(cancellationToken);

        if (items.Count == 0)
        {
            return ListDataResult<T>.Empty();
        }

        int? totalCount = query.IncludeTotalCount ? await source.CountAsync(cancellationToken) : null;
        var hasMoreData = items.Count > query.Take;

        if (hasMoreData)
        {
            items = items.Take(query.Take).ToList();
        }

        return new(items, hasMoreData, totalCount);
    }

    public static ListDataResult<T> AsListDataResult<T>(this IEnumerable<T> source, IBasicListQuery query)
    {
        IReadOnlyCollection<T> items;
        var sort = string.IsNullOrEmpty(query.Sort) ? $"-{nameof(IBasicEntity.CreatedAt)}" : query.Sort;


        if (query.Skip > 0)
        {
            items = source.OrderBySortStringParam(sort).Skip(query.Skip).Take(query.Take).ToList();
        }
        else
        {
            items = source.OrderBySortStringParam(sort).Take(query.Take).ToList();
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
