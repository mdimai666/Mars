using System.Collections.ObjectModel;
using AppFront.Main.Extensions;
using Mars.Shared.Contracts.PostCategoryTypes;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace AppAdmin.Pages.PostCategoryTypeViews;

public partial class ListPostCategoryTypePage
{
    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] AppFront.Shared.Interfaces.IMessageService _messageService { get; set; } = default!;

    string urlEditPage = "/dev/EditPostCategoryType";

    //table
    FluentDataGrid<PostCategoryTypeListItemResponse> table = default!;
    string _searchText = "";
    ListDataResult<PostCategoryTypeListItemResponse> data = ListDataResult<PostCategoryTypeListItemResponse>.Empty();
    GridItemsProvider<PostCategoryTypeListItemResponse> dataProvider = default!;

    protected override void OnParametersSet()
    {
        dataProvider = new GridItemsProvider<PostCategoryTypeListItemResponse>(
            async req =>
            {
                _ = req.SortByAscending;
                _ = req.SortByColumn;

                var sortColumn = req.GetSortByProperties().Count == 0 ? nameof(PostCategoryTypeListItemResponse.Title) : req.GetSortByProperties().First().PropertyName;

                var sort = (req.SortByAscending ? "" : "-") + sortColumn;

                data = await client.PostCategoryType.List(new()
                {
                    //Page = pagination.CurrentPageIndex + 1,
                    //PageSize = pagination.ItemsPerPage,
                    Skip = req.StartIndex,
                    Take = req.Count ?? BasicListQuery.DefaultPageSize,
                    Sort = sort,
                    Search = _searchText,
                });

                var collection = new Collection<PostCategoryTypeListItemResponse>(data.Items.ToList());

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
        await client.PostCategoryType.Delete(id).SmartDelete();
        _ = table.RefreshDataAsync();
    }
}
