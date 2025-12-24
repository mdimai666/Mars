using System.Collections.ObjectModel;
using Mars.Nodes.Front.Shared.Contracts.NodeTaskJob;
using Mars.Nodes.Front.Shared.Services;
using Mars.Shared.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Mars.Nodes.Workspace.Components.JobViews;

public partial class NodeTaskJobListView
{
    [Inject] INodeServiceClient _client { get; set; } = default!;
    [Inject] IDialogService _dialogService { get; set; } = default!;

    [Parameter] public EventCallback<NodeTaskResultSummaryResponse> OnClickItem { get; set; } = default!;

    //table
    FluentDataGrid<NodeTaskResultSummaryResponse> table = default!;
    string _searchText = "";
    ListDataResult<NodeTaskResultSummaryResponse> data = ListDataResult<NodeTaskResultSummaryResponse>.Empty();
    GridItemsProvider<NodeTaskResultSummaryResponse> dataProvider = default!;

    private static Icon _infoIcon = new Icons.Regular.Size16.Info();

    protected override void OnParametersSet()
    {

        dataProvider = new GridItemsProvider<NodeTaskResultSummaryResponse>(
            async req =>
            {
                _ = req.SortByAscending;
                _ = req.SortByColumn;

                var sortColumn = req.GetSortByProperties().Count == 0 ? nameof(NodeTaskResultSummaryResponse.StartDate) : req.GetSortByProperties().First().PropertyName;

                var sort = (req.SortByAscending ? "" : "-") + sortColumn;

                data = await _client.JobList(new()
                {
                    //Page = pagination.CurrentPageIndex + 1,
                    //PageSize = pagination.ItemsPerPage,
                    Skip = req.StartIndex,
                    Take = req.Count ?? BasicListQuery.DefaultPageSize,
                    Sort = sort,
                    Search = _searchText,
                });

                var collection = new Collection<NodeTaskResultSummaryResponse>(data.Items.ToList());

                StateHasChanged();

                return GridItemsProviderResult.From(collection, data.TotalCount ?? data.Items.Count);
            }
        );
        Refresh();
    }

    void HandleSearchInput()
    {
        table.RefreshDataAsync();
    }

    public void Refresh()
    {
        table?.RefreshDataAsync();
    }

    private void OnClickItemEvent(NodeTaskResultSummaryResponse task)
    {
        OnClickItem.InvokeAsync(task);
    }

    public static string GetStatusText(NodeTaskResultSummaryResponse task)
    {
        if (task.ErrorCount > 0) return "Error";
        if (task.IsTerminated) return "Terminated";
        if (task.IsDone) return "Done";
        return "Running";
    }

    public static string GetStatusColorFillName(NodeTaskResultSummaryResponse task)
    {
        if (task.ErrorCount > 0) return "error";
        if (task.IsTerminated) return "black";
        if (task.IsDone) return "success";
        return "info";
    }
}
