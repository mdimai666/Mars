using System.Collections.ObjectModel;
using Mars.Cms.Contracts.Feedbacks;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Pages.FeedbackViews;

public partial class FeedbackListPage
{
    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] Mars.Admin.Framework.Interfaces.IMessageService _messageService { get; set; } = default!;
    [Inject] IDialogService dialogService { get; set; } = default!;
    [Inject] AdminJs _appjs { get; set; } = default!;

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
                var sortBy = req.GetSortByProperties();
                var sortColumn = sortBy.Count == 0 ? nameof(FeedbackSummaryResponse.CreatedAt) : sortBy.First().PropertyName;

                var sort = ((req.SortColumns.FirstOrDefault()?.Ascending ?? false) ? "" : "-") + sortColumn;

                data = await client.Feedback.List(new()
                {
                    //Page = pagination.CurrentPageIndex + 1,
                    //PageSize = pagination.ItemsPerPage,
                    Skip = req.StartIndex,
                    Take = req.Count is > 0 ? req.Count.Value : BasicListQuery.DefaultPageSize,
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

        DialogOptions options = new()
        {
            Header = { Title = row.Item.Title },
            //PrimaryActionEnabled = false,
            //PrimaryAction = "Yes",
            //Width = "500px",
            //TrapFocus = _trapFocus,
            //Modal = _modal,
        };

        var detail = await client.Feedback.Get(row.Item.Id);

        if (detail is not null)
        {
            options.Parameters["Content"] = detail;
            DialogResult? result = await dialogService.ShowDialogAsync<ViewFeedbackDialog>(options);
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
        //string fileName = $"feedbacks-{DateTime.Now.ToString("yyyy-MM-dd")}.xlsx";

        await _appjs.DownloadFileFromUrl(url);
    }
}
