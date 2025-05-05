using System.Collections.ObjectModel;
using Mars.Shared.Contracts.Plugins;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AppAdmin.Pages.PluginViews;

public partial class PluginsListPage
{
    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] AppFront.Shared.Interfaces.IMessageService _messageService { get; set; } = default!;
    [Inject] IJSRuntime jSRuntime { get; set; } = default!;
    [Inject] IDialogService dialogService { get; set; } = default!;


    FluentDataGrid<PluginInfoResponse> table = default!;
    string _searchText = "";
    ListDataResult<PluginInfoResponse> data = ListDataResult<PluginInfoResponse>.Empty();
    GridItemsProvider<PluginInfoResponse> dataProvider = default!;

    protected override void OnParametersSet()
    {
        dataProvider = new GridItemsProvider<PluginInfoResponse>(
            async req =>
            {
                _ = req.SortByAscending;
                _ = req.SortByColumn;

                var sortColumn = req.GetSortByProperties().Count == 0 ? "Title" : req.GetSortByProperties().First().PropertyName;

                var sort = (req.SortByAscending ? "" : "-") + sortColumn;

                data = await client.Plugin.List(new()
                {
                    //Page = pagination.CurrentPageIndex + 1,
                    //PageSize = pagination.ItemsPerPage,
                    Skip = req.StartIndex,
                    Take = req.Count ?? BasicListQuery.DefaultPageSize,
                    Sort = sort,
                    Search = _searchText,
                });

                var collection = new Collection<PluginInfoResponse>(data.Items.ToList());

                StateHasChanged();

                return GridItemsProviderResult.From(collection, data.TotalCount ?? data.Items.Count);
            }
        );
    }

    void HandleSearchInput()
    {
        table.RefreshDataAsync();
    }

    async void OnRowClick(FluentDataGridRow<PluginInfoResponse> row)
    {

        if (row.Item is null) return;

        //DialogParameters parameters = new()
        //{
        //    Title = row.Item.Title,
        //    //PrimaryActionEnabled = false,
        //    //PrimaryAction = "Yes",
        //    SecondaryAction = null,
        //    //Width = "500px",
        //    //TrapFocus = _trapFocus,
        //    //Modal = _modal,
        //    PreventScroll = true
        //};

        //var detail = await client.Plugin.Get(row.Item.Id);

        //if (detail is not null)
        //{
        //    IDialogReference dialog = await dialogService.ShowDialogAsync<ViewFeedbackDialog>(detail, parameters);
        //    DialogResult? result = await dialog.Result;
        //}
        //else
        //{
        //    _ = _messageService.Error("element not found");
        //}

    }

    public async Task Delete(PluginInfoResponse context)
    {
        //await client.Feedback.Delete(id).SmartDelete();
        //_ = table.RefreshDataAsync();
    }

}
