using System.Collections.ObjectModel;
using AppFront.Main.Extensions;
using Mars.Shared.Contracts.PostCategories;
using Mars.Shared.Contracts.PostCategoryTypes;
using Mars.Shared.Contracts.PostTypes;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AppAdmin.Pages.PostCategoryViews;

public partial class ManagePostCategoryView
{
    [Inject] IMarsWebApiClient client { get; set; } = default!;

    [Parameter, EditorRequired]
    public PostTypeAdminPanelItemResponse PostType { get; set; } = default!;

    [Parameter] public EventCallback<PostCategoryListItemResponse> OnClickItem { get; set; } = default!;
    [Parameter] public EventCallback<PostCategoryListItemResponse> OnClickCreate { get; set; } = default!;
    [Parameter] public EventCallback<PostCategoryListItemResponse> OnDeleteItem { get; set; } = default!;

    string previousRequestPostTypeName = "";
    string urlEditPage = "/dev/EditPostCategory";

    //table
    FluentDataGrid<PostCategoryListItemResponse> table = default!;
    string _searchText = "";
    ListDataResult<PostCategoryListItemResponse> data = ListDataResult<PostCategoryListItemResponse>.Empty();
    GridItemsProvider<PostCategoryListItemResponse> dataProvider = default!;

    string prevPostTypeName = "";

    protected override void OnParametersSet()
    {
        if (prevPostTypeName != PostType.TypeName)
        {
            prevPostTypeName = PostType.TypeName;

            dataProvider = new GridItemsProvider<PostCategoryListItemResponse>(
                async req =>
                {
                    _ = req.SortByAscending;
                    _ = req.SortByColumn;

                    var sortColumn = req.GetSortByProperties().Count == 0 ? nameof(PostCategoryListItemResponse.SlugPath) : req.GetSortByProperties().First().PropertyName;

                    var sort = (req.SortByAscending ? "" : "-") + sortColumn;

                    data = await client.PostCategory.ListForPostType(PostType.TypeName, new()
                    {
                        //Page = pagination.CurrentPageIndex + 1,
                        //PageSize = pagination.ItemsPerPage,
                        Skip = req.StartIndex,
                        Take = req.Count ?? BasicListQuery.DefaultPageSize,
                        Sort = sort,
                        Search = _searchText,
                    });

                    var collection = new Collection<PostCategoryListItemResponse>(data.Items.ToList());

                    StateHasChanged();

                    return GridItemsProviderResult.From(collection, data.TotalCount ?? data.Items.Count);
                }
            );
            Refresh();
        }

        if (previousRequestPostTypeName != PostType.TypeName)
        {
            previousRequestPostTypeName = PostType.TypeName;
        }
    }

    void HandleSearchInput()
    {
        table.RefreshDataAsync();
    }

    void ClickItem(PostCategoryListItemResponse item)
    {
        OnClickItem.InvokeAsync(item);
    }

    Task ClickCreate()
    {
        return OnClickCreate.InvokeAsync();
    }

    async Task DeleteItem(PostCategoryListItemResponse item)
    {
        await client.PostCategory.Delete(item.Id).SmartDelete();
        _ = table.RefreshDataAsync();
        _ = OnDeleteItem.InvokeAsync(item);
    }

    public void Refresh()
    {
        table?.RefreshDataAsync();
    }

}
