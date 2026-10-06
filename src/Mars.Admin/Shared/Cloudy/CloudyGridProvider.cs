using System.Collections.ObjectModel;
using Mars.Contracts.Common;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Shared.Cloudy;

/// <summary>
/// Фабрика GridItemsProvider для cloudy-страниц: маппит запрос DataGrid
/// (skip/take/sort) на серверный List-вызов и результат на ListDataResult.
/// </summary>
public static class CloudyGridProvider
{
    /// <param name="loader">(skip, take, sort) → результат; sort = null — сортировка по умолчанию сервера</param>
    public static GridItemsProvider<T> Create<T>(
        Func<int, int, string?, Task<ListDataResult<T>>> loader,
        int defaultTake = 50)
    {
        return new GridItemsProvider<T>(async req =>
        {
            // v5: первый запрос виртуализации даёт Count=0 (не null)
            var take = req.Count is > 0 ? req.Count.Value : defaultTake;

            string? sort = null;
            var sortBy = req.GetSortByProperties();
            if (sortBy.Count != 0)
            {
                var ascending = req.SortColumns.FirstOrDefault()?.Ascending ?? true;
                sort = (ascending ? "" : "-") + sortBy.First().PropertyName;
            }

            var data = await loader(req.StartIndex, take, sort);
            var items = new Collection<T>(data.Items.ToList());
            return GridItemsProviderResult.From(items, data.TotalCount ?? items.Count);
        });
    }
}
