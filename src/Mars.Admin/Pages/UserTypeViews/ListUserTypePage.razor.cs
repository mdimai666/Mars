using Mars.Admin.Shared.Cloudy;
using Mars.Contracts.Common;
using Mars.Identity.Contracts.UserTypes;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Pages.UserTypeViews;

public partial class ListUserTypePage
{
    [Inject] IMarsWebApiClient client { get; set; } = default!;

    string UrlEditPage = "/dev/EditUserType";

    FluentDataGrid<UserTypeListItemResponse> _grid = default!;
    GridItemsProvider<UserTypeListItemResponse> _dataProvider = default!;

    string _searchText = "";
    int? _total;
    IReadOnlyDictionary<string, KpiResult> _kpi = new Dictionary<string, KpiResult>();

    protected override async Task OnInitializedAsync()
    {
        _dataProvider = CloudyGridProvider.Create<UserTypeListItemResponse>(LoadUserTypes);
        await LoadKpi();
    }

    async Task<ListDataResult<UserTypeListItemResponse>> LoadUserTypes(int skip, int take, string? sort)
    {
        var data = await client.UserType.List(new()
        {
            Skip = skip,
            Take = take,
            Sort = sort ?? $"-{nameof(UserTypeListItemResponse.CreatedAt)}",
            Search = string.IsNullOrWhiteSpace(_searchText) ? null : _searchText,
        });

        _total = data.TotalCount ?? data.Items.Count;
        StateHasChanged();
        return data;
    }

    async Task LoadKpi()
    {
        _kpi = await client.Kpi.Get([UserTypeKpiKeys.Total]);
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
        await client.UserType.Delete(id).SmartDelete();
        RefreshGrid();
        await LoadKpi();
    }
}
