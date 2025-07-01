using System.Collections.ObjectModel;
using AppFront.Main.Extensions;
using Mars.Shared.Contracts.UserTypes;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace AppAdmin.Pages.UserTypeViews;

public partial class ListUserTypePage
{
    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] AppFront.Shared.Interfaces.IMessageService _messageService { get; set; } = default!;

    string urlEditPage = "/dev/EditUserType";

    //table
    FluentDataGrid<UserTypeListItemResponse> table = default!;
    string _searchText = "";
    ListDataResult<UserTypeListItemResponse> data = ListDataResult<UserTypeListItemResponse>.Empty();
    GridItemsProvider<UserTypeListItemResponse> dataProvider = default!;

    protected override void OnParametersSet()
    {
        dataProvider = new GridItemsProvider<UserTypeListItemResponse>(
            async req =>
            {
                _ = req.SortByAscending;
                _ = req.SortByColumn;

                var sortColumn = req.GetSortByProperties().Count == 0 ? nameof(UserTypeListItemResponse.Title) : req.GetSortByProperties().First().PropertyName;

                var sort = (req.SortByAscending ? "" : "-") + sortColumn;

                data = await client.UserType.List(new()
                {
                    //Page = pagination.CurrentPageIndex + 1,
                    //PageSize = pagination.ItemsPerPage,
                    Skip = req.StartIndex,
                    Take = req.Count ?? BasicListQuery.DefaultPageSize,
                    Sort = sort,
                    Search = _searchText,
                });

                var collection = new Collection<UserTypeListItemResponse>(data.Items.ToList());

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
        await client.UserType.Delete(id).SmartDelete();
        _ = table.RefreshDataAsync();
    }
}
