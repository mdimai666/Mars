using System.Collections.ObjectModel;
using System.Text.Json;
using AppFront.Main.Extensions;
using AppFront.Shared.Extensions;
using AppFront.Shared.Services;
using Flurl.Http;
using Mars.Shared.Common;
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

    //table data
    FluentDataGrid<FileListItemResponse> table = default!;
    string _searchText = "";
    PagingResult<FileListItemResponse> data = PagingResult<FileListItemResponse>.Empty();
    GridItemsProvider<FileListItemResponse> dataProvider = default!;
    PaginationState pagination = new PaginationState { ItemsPerPage = PageSize };

    //FluentInputFileEventArgs[] Files2 = Array.Empty<FluentInputFileEventArgs>();

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
                    FolderId = _currentFolderId ?? Guid.Empty,
                });

                var collection = new Collection<FileListItemResponse>(data.Items.ToList());

                StateHasChanged();

                return GridItemsProviderResult.From(collection, data.TotalCount ?? data.Items.Count);
            }
        );
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadFolders();
    }

    //-------------------------------------------
    // Folders

    Guid? _currentFolderId = null;
    List<FolderResponse> _folders = [];
    List<FolderResponse> _breadcrumbs = [];
    bool _foldersLoading;
    bool IsRoot => _currentFolderId is null;

    /// <summary>Родитель текущей папки как цель перемещения (null когда уже в корне)</summary>
    FolderResponse? ParentFolderForMove => _breadcrumbs.Count > 1 ? _breadcrumbs[^2] : null;

    async Task LoadFolders()
    {
        _foldersLoading = true;
        StateHasChanged();

        _folders = await client.Media.ListFolders(_currentFolderId);
        _breadcrumbs = _currentFolderId is Guid folderId
            ? await client.Media.FolderBreadcrumbs(folderId)
            : [];

        _foldersLoading = false;
        StateHasChanged();
    }

    async Task OpenFolder(FolderResponse folder)
    {
        _currentFolderId = folder.Id;
        pagination = new PaginationState { ItemsPerPage = PageSize };
        await LoadFolders();
        _ = table.RefreshDataAsync();
    }

    async Task GoToRoot()
    {
        _currentFolderId = null;
        pagination = new PaginationState { ItemsPerPage = PageSize };
        await LoadFolders();
        _ = table.RefreshDataAsync();
    }

    // создание / переименование папки

    bool _visibleFolderNameModal;
    string _folderNameInput = "";
    string? _folderNameError;
    FolderResponse? _renamingFolder;

    void OpenCreateFolderDialog()
    {
        _renamingFolder = null;
        _folderNameInput = "";
        _folderNameError = null;
        _visibleFolderNameModal = true;
    }

    void OpenRenameFolderDialog(FolderResponse folder)
    {
        _renamingFolder = folder;
        _folderNameInput = folder.Name;
        _folderNameError = null;
        _visibleFolderNameModal = true;
    }

    async Task SubmitFolderName()
    {
        try
        {
            if (_renamingFolder is null)
            {
                await client.Media.CreateFolder(new CreateFolderRequest
                {
                    Name = _folderNameInput.Trim(),
                    ParentId = _currentFolderId,
                });
            }
            else
            {
                await client.Media.RenameFolder(_renamingFolder.Id, new RenameFolderRequest
                {
                    NewName = _folderNameInput.Trim(),
                });
            }

            _visibleFolderNameModal = false;
            await LoadFolders();
            _ = table.RefreshDataAsync();
        }
        catch (Exception ex)
        {
            _folderNameError = await ExtractErrorMessage(ex);
        }
    }

    async Task FolderDeleteClick(FolderResponse folder)
    {
        var ok = await _dialogService.MarsDeleteConfirmation();
        if (!ok) return;

        if (await client.Media.DeleteFolder(folder.Id).SmartDelete())
        {
            await LoadFolders();
        }
    }

    // перемещение файла

    bool _visibleMoveModal;
    FileListItemResponse? _movingFile;

    void OpenMoveDialog(FileListItemResponse file)
    {
        _movingFile = file;
        _visibleMoveModal = true;
    }

    async Task MoveTo(Guid? targetFolderId)
    {
        if (_movingFile is null) return;

        try
        {
            var result = await client.Media.MoveFiles(new MoveFilesRequest
            {
                Ids = [_movingFile.Id],
                FolderId = targetFolderId,
            });

            _visibleMoveModal = false;
            _movingFile = null;

            if (!result.Ok)
            {
                ShowActionResult(result);
                return;
            }

            await LoadFolders();
            _ = table.RefreshDataAsync();
        }
        catch (Exception ex)
        {
            _visibleMoveModal = false;
            ShowActionResult(new UserActionResult { Message = await ExtractErrorMessage(ex) });
        }
    }

    void ShowActionResult(UserActionResult result)
    {
        actionResult = result;
        _visibleActionModal = true;
        StateHasChanged();
    }

    static async Task<string> ExtractErrorMessage(Exception ex)
    {
        if (ex is FlurlHttpException httpEx)
        {
            try
            {
                var body = await httpEx.GetResponseStringAsync();
                var result = JsonSerializer.Deserialize<UserActionResult>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (!string.IsNullOrEmpty(result?.Message)) return result.Message;
            }
            catch
            {
                // тело не UserActionResult — показываем стандартное сообщение
            }
        }

        return ex.Message;
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

    void OnUploadCompleted(IReadOnlyCollection<FileDetailResponse> files)
    {
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
