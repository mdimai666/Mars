using System.Diagnostics.CodeAnalysis;
using Mars.Core.Extensions;
using Mars.Shared.Common;
using Mars.Shared.Models.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Shared.Dto.Common;

public static class PaginationExtensions
{
    public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> source, IBasicListQuery query)
    {
        if (query.Skip > 0)
        {
            return source.Skip(query.Skip).Take(query.Take + 1);
        }
        else
        {
            return source.Take(query.Take + 1);
        }
    }

    public static async Task<PagingResult<T>> ToPagingResult<T>(this IQueryable<T> source, IBasicListQuery query, CancellationToken cancellationToken)
    {
        var sort = string.IsNullOrEmpty(query.Sort) ? $"-{nameof(IBasicEntity.CreatedAt)}" : query.Sort;

        var items = await source.OrderBySortStringParam(sort).ApplyPaging(query).ToListAsync(cancellationToken);

        if (items.Count == 0)
        {
            return PagingResult<T>.Empty();
        }

        int? totalCount = query.IncludeTotalCount ? await source.CountAsync(cancellationToken) : null;
        var hasMoreData = items.Count > query.Take;

        if (hasMoreData)
        {
            items = items.Take(query.Take).ToList();
        }

        return new(items, query, hasMoreData, totalCount);
    }

    public static PagingResult<T> AsPagingResult<T>(this IEnumerable<T> source, IBasicListQuery query)
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
