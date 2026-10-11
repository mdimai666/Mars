using Mars.Admin.Shared.Cloudy;
using Mars.Cms.Contracts.PostCategoryTypes;
using Mars.Contracts.Common;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Pages.PostCategoryTypeViews;

public partial class ListPostCategoryTypePage
{
    [Inject] IMarsWebApiClient client { get; set; } = default!;

    string UrlEditPage = "/dev/EditPostCategoryType";

    FluentDataGrid<PostCategoryTypeListItemResponse> _grid = default!;
    GridItemsProvider<PostCategoryTypeListItemResponse> _dataProvider = default!;

    string _searchText = "";
    int? _total;
    IReadOnlyDictionary<string, KpiResult> _kpi = new Dictionary<string, KpiResult>();

    protected override async Task OnInitializedAsync()
    {
        _dataProvider = CloudyGridProvider.Create<PostCategoryTypeListItemResponse>(LoadCategoryTypes);
        await LoadKpi();
    }

    async Task<ListDataResult<PostCategoryTypeListItemResponse>> LoadCategoryTypes(int skip, int take, string? sort)
    {
        var data = await client.PostCategoryType.List(new()
        {
            Skip = skip,
            Take = take,
            Sort = sort ?? $"-{nameof(PostCategoryTypeListItemResponse.CreatedAt)}",
            Search = string.IsNullOrWhiteSpace(_searchText) ? null : _searchText,
        });

        _total = data.TotalCount ?? data.Items.Count;
        StateHasChanged();
        return data;
    }

    async Task LoadKpi()
    {
        _kpi = await client.Kpi.Get([PostCategoryTypeKpiKeys.Total]);
    }

    string KpiLabel(string key)
        => _kpi.TryGetValue(key, out var r) && !string.IsNullOrEmpty(r.Label) ? L[r.Label].Value : key;

    string KpiValue(string key)
        => _kpi.TryGetValue(key, out var r) ? r.Value.ToString("N0") : "—";

    void RefreshGrid() => _grid?.RefreshDataAsync();

    void OnSearchChanged(string text)
    {
        _searchText = text;
        RefreshGrid();
    }

    public async Task Delete(Guid id)
    {
        await client.PostCategoryType.Delete(id).SmartDelete();
        RefreshGrid();
        await LoadKpi();
    }
}
