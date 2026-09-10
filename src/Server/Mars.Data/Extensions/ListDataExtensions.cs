using Mars.Contracts.Common;
using Mars.Core.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Mars.Data.Extensions;

public static class ListDataExtensions
{
    public static async Task<ListDataResult<T>> ToListDataResult<T>(this IQueryable<T> source, IBasicListQuery query, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(query.Sort)) source = source.OrderBySortStringParam(query.Sort);

        var items = await source.ApplyPaging(query).ToListAsync(cancellationToken);

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
}
