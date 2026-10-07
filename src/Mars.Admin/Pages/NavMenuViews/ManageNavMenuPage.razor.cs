using Mars.Admin.Shared.Cloudy;
using Mars.Cms.Contracts.NavMenus;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Pages.NavMenuViews;

public partial class ManageNavMenuPage
{
    string urlEditPage = "/dev/EditNavMenu";

    [Inject] IMarsWebApiClient client { get; set; } = default!;

    FluentDataGrid<NavMenuSummaryResponse> _grid = default!;
    GridItemsProvider<NavMenuSummaryResponse> _dataProvider = default!;

    string _searchText = "";
    int? _total;

    protected override void OnInitialized()
    {
        _dataProvider = CloudyGridProvider.Create<NavMenuSummaryResponse>(LoadNavMenus);
    }

    async Task<ListDataResult<NavMenuSummaryResponse>> LoadNavMenus(int skip, int take, string? sort)
    {
        var data = await client.NavMenu.ListForAdmin(new()
        {
            Skip = skip,
            Take = take,
            Sort = sort ?? $"-{nameof(NavMenuSummaryResponse.CreatedAt)}",
            Search = string.IsNullOrWhiteSpace(_searchText) ? null : _searchText,
        });

        _total = data.TotalCount ?? data.Items.Count;
        StateHasChanged();
        return data;
    }

    void OnSearchChanged(string text)
    {
        _searchText = text;
        _grid?.RefreshDataAsync();
    }

    public async Task Delete(Guid id)
    {
        await client.NavMenu.Delete(id).SmartDelete();
        _grid?.RefreshDataAsync();
    }
}
