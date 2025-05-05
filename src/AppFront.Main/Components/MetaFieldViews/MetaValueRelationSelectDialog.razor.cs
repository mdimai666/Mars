using System.Collections.ObjectModel;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Resources;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace AppFront.Shared.Components.MetaFieldViews;

public partial class MetaValueRelationSelectDialog
{
    [Inject] IDialogService _dialogService { get; set; } = default!;
    [Inject] IMarsWebApiClient client { get; set; } = default!;


    [CascadingParameter]
    public FluentDialog Dialog { get; set; } = default!;

    [Parameter]
    public MetaValueRelationSelectDialogData Content { get; set; } = default!;

    [Parameter] public static int PageSize { get; set; } = 20;

    //table data
    FluentDataGrid<MetaValueRelationModelSummaryResponse> table = default!;
    string _searchText = "";
    ListDataResult<MetaValueRelationModelSummaryResponse> data = ListDataResult<MetaValueRelationModelSummaryResponse>.Empty();
    GridItemsProvider<MetaValueRelationModelSummaryResponse> dataProvider = default!;
    //PaginationState pagination = new PaginationState { ItemsPerPage = PageSize };

    public static Dictionary<string, string> SortOptions = new()
    {
        ["CreatedAt"] = AppRes.ByDate,
        //["FileName"] = AppRes.Name,
        //["FileSize"] = AppRes.FileSize,
        ["Title"] = AppRes.Title,
    };

    string _sortValue = nameof(MetaValueRelationModelSummaryResponse.CreatedAt);
    bool _sortDirectionDesc = true;
    static Icon iconSortDown = new Icons.Regular.Size16.ArrowSortDownLines();
    static Icon iconSortUp = new Icons.Regular.Size16.ArrowSortUpLines();
    Icon sortButtonIcon => _sortDirectionDesc ? iconSortDown : iconSortUp;

    protected override void OnParametersSet()
    {
        dataProvider = new GridItemsProvider<MetaValueRelationModelSummaryResponse>(
            async req =>
            {
                _ = req.SortByAscending;
                _ = req.SortByColumn;

                //var sortColumn = req.GetSortByProperties().Count == 0
                //                        ? nameof(MetaValueRelationModelSummaryResponse.CreatedAt)
                //                        : req.GetSortByProperties().First().PropertyName;
                //var sort = (req.SortByAscending ? "" : "-") + sortColumn;

                var sortColumn = _sortValue;
                var sort = (_sortDirectionDesc ? "-" : "") + sortColumn;

                data = await client.PostType.ListMetaValueRelationModels(new()
                {
                    //Page = pagination.CurrentPageIndex + 1,
                    //PageSize = pagination.ItemsPerPage,
                    Skip = req.StartIndex,
                    Take = req.Count ?? BasicListQuery.DefaultPageSize,
                    Sort = sort,
                    Search = _searchText,
                    ModelName = Content.ModelName,
                });

                var collection = new Collection<MetaValueRelationModelSummaryResponse>(data.Items.ToList());

                StateHasChanged();

                return GridItemsProviderResult.From(collection, data.TotalCount ?? data.Items.Count);
            }
        );
    }

    void HandleSearchInput()
    {
        table.RefreshDataAsync();
    }

    async void OnRowClick(FluentDataGridRow<MetaValueRelationModelSummaryResponse> row)
    {
        if (row.Item is null) return;

        await Dialog.CloseAsync(row.Item);
    }

    void SelectSortOption(KeyValuePair<string, string> value)
    {
        HandleSearchInput();
    }

    void OnClickSortDirectionChange()
    {
        _sortDirectionDesc = !_sortDirectionDesc;
        HandleSearchInput();
    }

    //private Task OkAsync() => Dialog.CloseAsync();
    private Task CancelAsync() => Dialog.CancelAsync();
}

public class MetaValueRelationSelectDialogData
{
    public required Guid ValueId { get; init; }
    public required string ModelName { get; init; }
}
