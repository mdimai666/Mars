using System.Collections.ObjectModel;
using AppFront.Main.Extensions;
using AppFront.Shared.Extensions;
using AppFront.Shared.Services;
using Mars.Core.Exceptions;
using Mars.Shared.Contracts.Files;
using Mars.Shared.Resources;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace AppFront.Shared.Components.MediaViews;

public partial class FluentMediaFilesList
{
    [Inject] IDialogService _dialogService { get; set; } = default!;
    [Inject] IAppMediaService mediaService { get; set; } = default!;
    [Inject] IMarsWebApiClient client { get; set; } = default!;

    [Parameter] public bool ReadOnly { get; set; } = false;
    [Parameter] public string Accept { get; set; } = "*";
    [Parameter] public RenderFragment<FileListItemResponse>? ItemActionBottom { get; set; } = null;
    [Parameter] public static int PageSize { get; set; } = 55;

    public string? ViewFiltergroup { get; set; } = null;
    public const string AllowExternsionsDefault = ".jpg,.png,.jpeg,.doc,.docx,.ppt,.pptx,.xls,.xlsx,.pdf,.jfif,.svg,.heic";
    string _fluentInputFileElementId = "my-file-uploader_" + Guid.NewGuid();

    //table data
    FluentDataGrid<FileListItemResponse> table = default!;
    string _searchText = "";
    PagingResult<FileListItemResponse> data = PagingResult<FileListItemResponse>.Empty();
    GridItemsProvider<FileListItemResponse> dataProvider = default!;
    PaginationState pagination = new PaginationState { ItemsPerPage = PageSize };

    //upload data
    int ProgressPercent = 0;
    //FluentInputFileEventArgs[] Files2 = Array.Empty<FluentInputFileEventArgs>();

    List<FileUploadResult> fileUploadResults = new();

    //[Parameter]
    //public EventCallback<List<FileListItemResponse>> FilesChanged { get; set; }

    public static Dictionary<string, string> SortOptions = new()
    {
        ["CreatedAt"] = AppRes.ByDate,
        ["FileName"] = AppRes.Name,
        ["FileSize"] = AppRes.FileSize,
    };

    string _sortValue = nameof(FileListItemResponse.CreatedAt);
    bool _sortDirectionDesc = true;
    static Icon iconSortDown = new Icons.Regular.Size16.ArrowSortDownLines();
    static Icon iconSortUp = new Icons.Regular.Size16.ArrowSortUpLines();
    Icon sortButtonIcon => _sortDirectionDesc ? iconSortDown : iconSortUp;

    protected override void OnParametersSet()
    {
        dataProvider = new GridItemsProvider<FileListItemResponse>(
            async req =>
            {
                _ = req.SortByAscending;
                _ = req.SortByColumn;

                //var sortColumn = req.GetSortByProperties().Count == 0
                //                        ? nameof(FileListItemResponse.CreatedAt)
                //                        : req.GetSortByProperties().First().PropertyName;
                //var sort = (req.SortByAscending ? "" : "-") + sortColumn;

                var sortColumn = _sortValue;
                var sort = (_sortDirectionDesc ? "-" : "") + sortColumn;

                data = await mediaService.ListTable(new()
                {
                    Page = pagination.CurrentPageIndex + 1,
                    PageSize = pagination.ItemsPerPage,
                    //Skip = req.StartIndex,
                    //Take = req.Count ?? BasicListQuery.DefaultPageSize,
                    Sort = sort,
                    Search = _searchText,
                });

                var collection = new Collection<FileListItemResponse>(data.Items.ToList());

                StateHasChanged();

                return GridItemsProviderResult.From(collection, data.TotalCount ?? data.Items.Count);
            }
        );
    }

    void HandleSearchInput()
    {
        table.RefreshDataAsync();
    }

    void OnRowClick(FluentDataGridRow<FileListItemResponse> row)
    {

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

    public async Task Delete(Guid id)
    {
        await mediaService.Delete(id).SmartDelete();
        _ = table.RefreshDataAsync();
    }

    async Task ItemRemoveClick(FileListItemResponse file)
    {
        var ok = await _dialogService.MarsDeleteConfirmation();

        if (ok)
        {
            await client.Media.Delete(file.Id).SmartDelete();
            _ = table.RefreshDataAsync();
        }
    }

    //-------------------------------------------
    // Upload

    class FileUploadResult
    {
        public string Name { get; init; }
        public long Size { get; init; }
        public string? ErrorMessage { get; init; }

        public FileUploadResult(string name, ulong size, string? errorMessage)
        {
            Name = name;
            Size = (long)size;
            ErrorMessage = errorMessage;
        }
    }

    private async Task OnFileUploaded(FluentInputFileEventArgs file)
    {
        try
        {
            var x = await client.Media.Upload(file.Stream!, file.Name);
            var result = new FileUploadResult(x.Name, x.Size, null);
            fileUploadResults.Add(result);
        }
        catch (MarsValidationException ex)
        {
            fileUploadResults.Add(new FileUploadResult(file.Name, (ulong)file.Size, string.Join("; ", ex.Errors.Values.SelectMany(x => x))));
        }
        catch (Exception ex)
        {
            fileUploadResults.Add(new FileUploadResult(file.Name, (ulong)file.Size, ex.Message));
        }
    }

    private void OnCompletedAsync(IEnumerable<FluentInputFileEventArgs> files)
    {
        //ProgressPercent = 0;
        HandleSearchInput();
    }

    //-------------------------------------------
    // Action

    bool _visibleActionModal;
    bool _loadingActionExecuting;
    UserActionResult? actionResult;

    async void ExecuteAction(string actionId, Dictionary<string, string>? args = null)
    {
        _visibleActionModal = true;
        actionResult = null;

        _loadingActionExecuting = true;
        StateHasChanged();

        actionResult = await mediaService.ExecuteAction(new ExecuteActionRequest { ActionId = actionId, Arguments = args ?? [] });

        _loadingActionExecuting = false;
        StateHasChanged();
    }
}
