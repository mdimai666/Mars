using Mars.Admin.Shared.Cloudy;
using Mars.Cms.Contracts.PostTypes;
using Mars.Contracts.Common;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Pages.PostTypeViews;

public partial class ListPostTypePage
{
    [Inject] IMarsWebApiClient client { get; set; } = default!;

    string UrlEditPage = "/dev/EditPostType";

    FluentDataGrid<PostTypeListItemResponse> _grid = default!;
    GridItemsProvider<PostTypeListItemResponse> _dataProvider = default!;

    string _searchText = "";
    bool _includeComponent;
    int? _total;
    IReadOnlyDictionary<string, KpiResult> _kpi = new Dictionary<string, KpiResult>();

    protected override async Task OnInitializedAsync()
    {
        _dataProvider = CloudyGridProvider.Create<PostTypeListItemResponse>(LoadPostTypes);
        await LoadKpi();
    }

    async Task<ListDataResult<PostTypeListItemResponse>> LoadPostTypes(int skip, int take, string? sort)
    {
        var data = await client.PostType.List(new()
        {
            Skip = skip,
            Take = take,
            Sort = sort ?? $"-{nameof(PostTypeListItemResponse.CreatedAt)}",
            Search = string.IsNullOrWhiteSpace(_searchText) ? null : _searchText,
            IncludeComponent = _includeComponent,
        });

        _total = data.TotalCount ?? data.Items.Count;
        StateHasChanged();
        return data;
    }

    async Task LoadKpi()
    {
        _kpi = await client.Kpi.Get(
        [
            PostTypeKpiKeys.Total,
            PostTypeKpiKeys.ComponentTypes,
            PostTypeKpiKeys.MetaFields,
            PostTypeKpiKeys.MetaValues,
        ]);
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

    void HandleIncludeComponentChanged(bool value)
    {
        _includeComponent = value;
        RefreshGrid();
    }

    public async Task Delete(Guid id)
    {
        await client.PostType.Delete(id).SmartDelete();
        RefreshGrid();
        await LoadKpi();
    }
}
