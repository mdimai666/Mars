using System.Collections.ObjectModel;
using AppFront.Main.Extensions;
using Mars.Shared.Contracts.Posts;
using Mars.Shared.Contracts.PostTypes;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AppAdmin.Pages.PostsViews;

public partial class ManagePostView
{

    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] AppFront.Shared.Interfaces.IMessageService _messageService { get; set; } = default!;
    [Inject] IJSRuntime jSRuntime { get; set; } = default!;
    [Inject] IDialogService dialogService { get; set; } = default!;

    [Parameter, EditorRequired]
    public PostTypeSummaryResponse PostType { get; set; } = default!;


    string urlEditPage = "/dev/EditPost";

    //table
    FluentDataGrid<PostListItemResponse> table = default!;
    string _searchText = "";
    ListDataResult<PostListItemResponse> data = ListDataResult<PostListItemResponse>.Empty();
    GridItemsProvider<PostListItemResponse> dataProvider = default!;

    string prevPostTypeName = "";

    protected override void OnParametersSet()
    {
        if (prevPostTypeName != PostType.TypeName)
        {
            prevPostTypeName = PostType.TypeName;

            dataProvider = new GridItemsProvider<PostListItemResponse>(
                async req =>
                {
                    _ = req.SortByAscending;
                    _ = req.SortByColumn;

                    var sortColumn = req.GetSortByProperties().Count == 0 ? nameof(PostListItemResponse.CreatedAt) : req.GetSortByProperties().First().PropertyName;

                    var sort = (req.SortByAscending ? "" : "-") + sortColumn;

                    data = await client.Post.List(PostType.TypeName, new()
                    {
                        //Page = pagination.CurrentPageIndex + 1,
                        //PageSize = pagination.ItemsPerPage,
                        Skip = req.StartIndex,
                        Take = req.Count ?? BasicListQuery.DefaultPageSize,
                        Sort = sort,
                        Search = _searchText,
                    });

                    var collection = new Collection<PostListItemResponse>(data.Items.ToList());

                    StateHasChanged();

                    return GridItemsProviderResult.From(collection, data.TotalCount ?? data.Items.Count);
                }
            );
            Refresh();
        }
    }

    void HandleSearchInput()
    {
        table.RefreshDataAsync();
    }

    async Task Delete(Guid id)
    {
        await client.Post.Delete(id).SmartDelete();
        _ = table.RefreshDataAsync();
    }

    public void Refresh()
    {
        table?.RefreshDataAsync();
    }

}
