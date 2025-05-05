using System.Collections.ObjectModel;
using AppFront.Main.Extensions;
using Mars.Shared.Contracts.PostTypes;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AppAdmin.Pages.PostTypeViews;

public partial class ListPostTypePage
{
    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] AppFront.Shared.Interfaces.IMessageService _messageService { get; set; } = default!;
    [Inject] IJSRuntime jSRuntime { get; set; } = default!;
    [Inject] IDialogService dialogService { get; set; } = default!;

    string urlEditPage = "/dev/EditPostType";

    //table
    FluentDataGrid<PostTypeListItemResponse> table = default!;
    string _searchText = "";
    ListDataResult<PostTypeListItemResponse> data = ListDataResult<PostTypeListItemResponse>.Empty();
    GridItemsProvider<PostTypeListItemResponse> dataProvider = default!;

    protected override void OnParametersSet()
    {
        dataProvider = new GridItemsProvider<PostTypeListItemResponse>(
            async req =>
            {
                _ = req.SortByAscending;
                _ = req.SortByColumn;

                var sortColumn = req.GetSortByProperties().Count == 0 ? nameof(PostTypeListItemResponse.Title) : req.GetSortByProperties().First().PropertyName;

                var sort = (req.SortByAscending ? "" : "-") + sortColumn;

                data = await client.PostType.List(new()
                {
                    //Page = pagination.CurrentPageIndex + 1,
                    //PageSize = pagination.ItemsPerPage,
                    Skip = req.StartIndex,
                    Take = req.Count ?? BasicListQuery.DefaultPageSize,
                    Sort = sort,
                    Search = _searchText,
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

    public async Task Delete(Guid id)
    {
        await client.PostType.Delete(id).SmartDelete();
        _ = table.RefreshDataAsync();
    }
}
