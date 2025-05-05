using System.Collections.ObjectModel;
using AppFront.Main.Extensions;
using Mars.Shared.Contracts.Feedbacks;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AppAdmin.Pages.FeedbackViews;

public partial class FeedbackListPage
{
    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] AppFront.Shared.Interfaces.IMessageService _messageService { get; set; } = default!;
    [Inject] IJSRuntime jSRuntime { get; set; } = default!;
    [Inject] IDialogService dialogService { get; set; } = default!;


    FluentDataGrid<FeedbackSummaryResponse> table = default!;
    string _searchText = "";
    ListDataResult<FeedbackSummaryResponse> data = ListDataResult<FeedbackSummaryResponse>.Empty();

    GridItemsProvider<FeedbackSummaryResponse> dataProvider = default!;
    //PaginationState pagination = new PaginationState { ItemsPerPage = 5 };

    protected override void OnParametersSet()
    {
        dataProvider = new GridItemsProvider<FeedbackSummaryResponse>(
            async req =>
            {
                _ = req.SortByAscending;
                _ = req.SortByColumn;

                var sortColumn = req.GetSortByProperties().Count == 0 ? nameof(FeedbackSummaryResponse.CreatedAt) : req.GetSortByProperties().First().PropertyName;

                var sort = (req.SortByAscending ? "" : "-") + sortColumn;

                data = await client.Feedback.List(new()
                {
                    //Page = pagination.CurrentPageIndex + 1,
                    //PageSize = pagination.ItemsPerPage,
                    Skip = req.StartIndex,
                    Take = req.Count ?? BasicListQuery.DefaultPageSize,
                    Sort = sort,
                    Search = _searchText,
                });

                var collection = new Collection<FeedbackSummaryResponse>(data.Items.ToList());

                StateHasChanged();

                return GridItemsProviderResult.From(collection, data.TotalCount ?? data.Items.Count);
            }
        );
    }

    void HandleSearchInput()
    {
        table.RefreshDataAsync();
    }

    async void OnRowClick(FluentDataGridRow<FeedbackSummaryResponse> row)
    {

        if (row.Item is null) return;

        DialogParameters parameters = new()
        {
            Title = row.Item.Title,
            //PrimaryActionEnabled = false,
            //PrimaryAction = "Yes",
            SecondaryAction = null,
            //Width = "500px",
            //TrapFocus = _trapFocus,
            //Modal = _modal,
            PreventScroll = true
        };

        var detail = await client.Feedback.Get(row.Item.Id);

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
        await client.Feedback.Delete(id).SmartDelete();
        _ = table.RefreshDataAsync();
    }

    async void DownloadExcel()
    {
        string url = Q.ServerUrlJoin("api/Feedback/DownloadExcel");
        string fileName = "";
        await jSRuntime.InvokeVoidAsync("MarsTriggerFileDownload", fileName, url);

    }
}
