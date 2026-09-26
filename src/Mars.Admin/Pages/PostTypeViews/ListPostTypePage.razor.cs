using System.Collections.ObjectModel;
using Mars.Cms.Contracts.PostTypes;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Mars.Admin.Pages.PostTypeViews;

public partial class ListPostTypePage
{
    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] Mars.Admin.Framework.Interfaces.IMessageService _messageService { get; set; } = default!;
    [Inject] IJSRuntime jSRuntime { get; set; } = default!;
    [Inject] IDialogService dialogService { get; set; } = default!;

    string urlEditPage = "/dev/EditPostType";

    //table
    FluentDataGrid<PostTypeListItemResponse> table = default!;
    string _searchText = "";
    bool _includeComponent = false;
    ListDataResult<PostTypeListItemResponse> data = ListDataResult<PostTypeListItemResponse>.Empty();
    GridItemsProvider<PostTypeListItemResponse> dataProvider = default!;

    protected override void OnParametersSet()
    {
        dataProvider = new GridItemsProvider<PostTypeListItemResponse>(
            async req =>
            {
                var sortBy = req.GetSortByProperties();
                var sortColumn = sortBy.Count == 0 ? nameof(PostTypeListItemResponse.Title) : sortBy.First().PropertyName;

                var sort = ((req.SortColumns.FirstOrDefault()?.Ascending ?? false) ? "" : "-") + sortColumn;

                data = await client.PostType.List(new()
                {
                    //Page = pagination.CurrentPageIndex + 1,
                    //PageSize = pagination.ItemsPerPage,
                    Skip = req.StartIndex,
                    Take = req.Count is > 0 ? req.Count.Value : BasicListQuery.DefaultPageSize,
                    Sort = sort,
                    Search = _searchText,
                    IncludeComponent = _includeComponent,
                });

                var collection = new Collection<PostTypeListItemResponse>(data.Items.ToList());

                StateHasChanged();

                return GridItemsProviderResult.From(collection, data.TotalCount ?? data.Items.Count);
            }
        );
    }

    void HandleSearchInput()
    {
        table.RefreshDataAsync();
    }

    void HandleIncludeComponentChanged(bool value)
    {
        _includeComponent = value;
        _ = table.RefreshDataAsync();
    }

    public async Task Delete(Guid id)
    {
        await client.PostType.Delete(id).SmartDelete();
        _ = table.RefreshDataAsync();
    }
}
