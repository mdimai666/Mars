using System.Collections.ObjectModel;
using Mars.Admin.Framework.Dialogs;
using Mars.Admin.Pages.FeedbackViews;
using Mars.Cms.Contracts.NavMenus;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Pages.NavMenuViews;

public partial class ManageNavMenuPage
{
    string urlEditPage = "/dev/EditNavMenu";

    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] Mars.Admin.Framework.Interfaces.IMessageService _messageService { get; set; } = default!;
    [Inject] IDialogService dialogService { get; set; } = default!;

    FluentDataGrid<NavMenuSummaryResponse> table = default!;
    string _searchText = "";
    ListDataResult<NavMenuSummaryResponse> data = ListDataResult<NavMenuSummaryResponse>.Empty();

    GridItemsProvider<NavMenuSummaryResponse> dataProvider = default!;
    //PaginationState pagination = new PaginationState { ItemsPerPage = 5 };

    protected override void OnParametersSet()
    {
        dataProvider = new GridItemsProvider<NavMenuSummaryResponse>(
            async req =>
            {
                var sortBy = req.GetSortByProperties();
                var sortColumn = sortBy.Count == 0
                                        ? nameof(NavMenuSummaryResponse.CreatedAt)
                                        : sortBy.First().PropertyName;

                var sort = ((req.SortColumns.FirstOrDefault()?.Ascending ?? false) ? "" : "-") + sortColumn;

                data = await client.NavMenu.ListForAdmin(new()
                {
                    //Page = pagination.CurrentPageIndex + 1,
                    //PageSize = pagination.ItemsPerPage,
                    Skip = req.StartIndex,
                    Take = req.Count ?? BasicListQuery.DefaultPageSize,
                    Sort = sort,
                    Search = _searchText,
                });

                var collection = new Collection<NavMenuSummaryResponse>(data.Items.ToList());

                StateHasChanged();

                return GridItemsProviderResult.From(collection, data.TotalCount ?? data.Items.Count);
            }
        );
    }

    void HandleSearchInput()
    {
        table.RefreshDataAsync();
    }

    async void OnRowClick(FluentDataGridRow<NavMenuSummaryResponse> row)
    {

        if (row.Item is null) return;
        return;

        DialogParameters parameters = new()
        {
            Title = row.Item.Title,
            //PrimaryActionEnabled = false,
            //PrimaryAction = "Yes",
            //Width = "500px",
            //TrapFocus = _trapFocus,
            //Modal = _modal,
            PreventScroll = true
        };

        var detail = await client.NavMenu.Get(row.Item.Id);

        if (detail is not null)
        {
            IDialogReference dialog = await dialogService.ShowDialogAsync<ViewFeedbackDialog>(detail, parameters);
            DialogResult? result = await dialog.Result;
        }
        else
        {
            _ = _messageService.Error("element not found");
        }

    }

    public async Task Delete(Guid id)
    {
        await client.NavMenu.Delete(id).SmartDelete();
        _ = table.RefreshDataAsync();
    }

}
