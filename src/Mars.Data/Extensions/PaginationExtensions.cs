using Mars.Contracts.Common;
using Mars.Core.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Mars.Data.Extensions;

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
        if (!string.IsNullOrEmpty(query.Sort)) source = source.OrderBySortStringParam(query.Sort);

        var items = await source.ApplyPaging(query).ToListAsync(cancellationToken);

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
}
