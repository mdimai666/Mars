using Mars.Admin.Framework.Extensions;
using Mars.Contracts.Common;
using Mars.Core.Extensions;
using Mars.Media.Contracts.Files;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Mars.Admin.Pages.MediaViews;

public partial class ManageMediaPage
{
    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] IDialogService _dialogService { get; set; } = default!;
    [Inject] Mars.Admin.Framework.Interfaces.IMessageService _messageService { get; set; } = default!;
    [Inject] IJSRuntime JS { get; set; } = default!;

    const int PageSize = 50;

    static readonly (string Field, string Label)[] SortOptions =
    [
        ("CreatedAt", "Date"),
        ("FileName", "Name"),
        ("FileSize", "Size"),
    ];

    Guid? _currentFolderId;
    List<FolderResponse> _folders = [];
    List<FolderResponse> _breadcrumbs = [];
    FolderResponse? ParentFolderForMove => _breadcrumbs.Count > 1 ? _breadcrumbs[^2] : null;

    IReadOnlyCollection<FileListItemResponse> _files = [];
    int _filesTotal;
    int _skip;
    bool _filesLoading;
    string _searchText = "";
    IReadOnlyDictionary<string, KpiResult> _kpi = new Dictionary<string, KpiResult>();

    bool _flatView;
    string _sortField = "CreatedAt";
    bool _sortDesc = true;

    FileListItemResponse? _propsFile;
    string _propsAnchorId = "";
    bool _propsOpen;

    protected override async Task OnInitializedAsync()
    {
        await OpenYearFolder();
        await Task.WhenAll(LoadFolders(), LoadFiles(), LoadKpi());
    }

    async Task OpenYearFolder()
    {
        try
        {
            var year = DateTimeOffset.Now.Year.ToString();
            var roots = await client.Media.ListFolders(null);
            var yearFolder = roots.FirstOrDefault(f => f.Name == year)
                             ?? await client.Media.CreateFolder(new CreateFolderRequest { Name = year, ParentId = null });
            _currentFolderId = yearFolder.Id;
        }
        catch
        {
            // папка года — удобство по умолчанию: при любой ошибке остаёмся в корне
        }
    }

    async Task LoadFolders()
    {
        if (_flatView)
        {
            _folders = [];
            _breadcrumbs = [];
            return;
        }

        _folders = await client.Media.ListFolders(_currentFolderId);
        _breadcrumbs = _currentFolderId is Guid folderId
            ? await client.Media.FolderBreadcrumbs(folderId)
            : [];
    }

    async Task LoadFiles()
    {
        _filesLoading = true;
        try
        {
            var data = await client.Media.List(new ListFileQueryRequest
            {
                Skip = _skip,
                Take = PageSize,
                Sort = (_sortDesc ? "-" : "") + _sortField,
                Search = string.IsNullOrWhiteSpace(_searchText) ? null : _searchText,
                FolderId = _flatView ? null : _currentFolderId ?? Guid.Empty,
            });
            _files = data.Items;
            _filesTotal = data.TotalCount ?? data.Items.Count;
        }
        finally
        {
            _filesLoading = false;
        }
    }

    async Task LoadKpi()
    {
        _kpi = await client.Kpi.Get([MediaKpiKeys.Files, MediaKpiKeys.Storage]);
    }

    string KpiLabel(string key)
        => _kpi.TryGetValue(key, out var r) && !string.IsNullOrEmpty(r.Label) ? L[r.Label].Value : key;

    string KpiValue(string key)
        => _kpi.TryGetValue(key, out var r) ? r.Value.ToString("N0") : "—";

    string StorageValue()
        => _kpi.TryGetValue(MediaKpiKeys.Storage, out var r) ? ((long)r.Value).ToHumanizedSize() : "—";

    async Task ToggleView()
    {
        _flatView = !_flatView;
        _skip = 0;
        if (_flatView)
        {
            _sortField = "CreatedAt";
            _sortDesc = true;
        }

        await Task.WhenAll(LoadFolders(), LoadFiles());
    }

    async Task SetSort(string field)
    {
        if (_sortField == field)
        {
            _sortDesc = !_sortDesc;
        }
        else
        {
            _sortField = field;
            _sortDesc = true;
        }

        _skip = 0;
        await LoadFiles();
    }

    async Task OnFileClick(FileListItemResponse file)
    {
        if (file.IsImage || file.IsSvg) return;

        if (MediaTextPreview.IsPreviewable(file))
        {
            await OpenTextDialog(file);
        }
        else
        {
            await OpenFileProps(file);
        }
    }

    async Task OpenTextDialog(FileListItemResponse file)
    {
        TextPreviewResult preview;
        try
        {
            preview = await JS.InvokeAsync<TextPreviewResult>("blazor_fetch_text", file.Url, MediaTextPreview.MaxSize, null);
        }
        catch (Exception ex)
        {
            _ = _messageService.Error($"Failed to load file text: {ex.Message}");
            return;
        }

        await _dialogService.ShowDialogAsync<MediaTextFileDialog>(new DialogOptions
        {
            Modal = true,
            Header = { CloseAction = { Visible = true } },
            Parameters =
            {
                ["DialogTitle"] = file.Name,
                ["Content"] = preview.Text,
                ["ContentEncoding"] = preview.Encoding,
                ["FileUrl"] = file.Url,
                ["MaxChars"] = MediaTextPreview.MaxSize,
            },
        });
    }

    async Task OpenFileProps(FileListItemResponse file)
    {
        if (_propsOpen)
        {
            _propsOpen = false;
            StateHasChanged();
            await Task.Delay(50);
        }

        _propsFile = file;
        _propsAnchorId = $"media-file-{file.Id}";
        _propsOpen = true;
    }

    async Task OpenFolder(FolderResponse folder)
    {
        _currentFolderId = folder.Id;
        _skip = 0;
        await Task.WhenAll(LoadFolders(), LoadFiles());
    }

    async Task GoToRoot()
    {
        _currentFolderId = null;
        _skip = 0;
        await Task.WhenAll(LoadFolders(), LoadFiles());
    }

    async Task OnSkipChanged(int skip)
    {
        _skip = skip;
        await LoadFiles();
    }

    async Task OnSearchChanged(string text)
    {
        _searchText = text;
        _skip = 0;
        await LoadFiles();
    }

    async Task OpenCreateFolderDialog()
    {
        var result = await _dialogService.ShowDialogAsync<MediaFolderNameDialog>(FolderDialogOptions("New folder", "",
            name => client.Media.CreateFolder(new CreateFolderRequest { Name = name, ParentId = _currentFolderId })));
        if (result.Cancelled || result.Value is not true) return;

        await LoadFolders();
    }

    async Task OpenCreateTextFileDialog()
    {
        var result = await _dialogService.ShowDialogAsync<MediaCreateTextFileDialog>(new DialogOptions
        {
            Modal = true,
            Header = { CloseAction = { Visible = true } },
            Parameters = { ["FolderId"] = _flatView ? null : _currentFolderId },
        });
        if (result.Cancelled || result.Value is not true) return;

        await Task.WhenAll(LoadFolders(), LoadFiles(), LoadKpi());
    }

    async Task TriggerUploadPicker()
    {
        await JS.InvokeVoidAsync("blazor_click_element", "media-upload-input");
    }

    async Task OpenRenameFolderDialog(FolderResponse folder)
    {
        var result = await _dialogService.ShowDialogAsync<MediaFolderNameDialog>(FolderDialogOptions("Rename folder", folder.Name,
            name => client.Media.RenameFolder(folder.Id, new RenameFolderRequest { NewName = name })));
        if (result.Cancelled || result.Value is not true) return;

        await LoadFolders();
    }

    static DialogOptions FolderDialogOptions(string title, string name, Func<string, Task> submit) => new()
    {
        Modal = true,
        Header = { CloseAction = { Visible = true } },
        Parameters = { ["DialogTitle"] = title, ["FolderName"] = name, ["SubmitAction"] = submit },
    };

    async Task DeleteFolder(FolderResponse folder)
    {
        if (!await _dialogService.MarsDeleteConfirmation()) return;

        if (await client.Media.DeleteFolder(folder.Id).SmartDelete())
        {
            await LoadFolders();
        }
    }

    async Task OpenMoveDialog(FileListItemResponse file)
    {
        var result = await _dialogService.ShowDialogAsync<MediaMoveFileDialog>(new DialogOptions
        {
            Modal = true,
            Header = { CloseAction = { Visible = true } },
            Parameters =
            {
                ["File"] = file,
                ["Folders"] = _folders,
                ["ParentFolder"] = ParentFolderForMove,
            },
        });
        if (result.Cancelled || result.Value is not Guid target) return;

        var move = await client.Media.MoveFiles(new MoveFilesRequest
        {
            Ids = [file.Id],
            FolderId = target == Guid.Empty ? null : target,
        });
        if (!move.Ok)
        {
            _ = _messageService.Error(move.Message);
            return;
        }

        await Task.WhenAll(LoadFolders(), LoadFiles());
    }

    async Task DeleteFile(FileListItemResponse file)
    {
        if (!await _dialogService.MarsDeleteConfirmation()) return;

        await client.Media.Delete(file.Id).SmartDelete();
        await Task.WhenAll(LoadFolders(), LoadFiles(), LoadKpi());
    }

    async Task OnFilesUploaded(IReadOnlyCollection<FileDetailResponse> files)
    {
        await Task.WhenAll(LoadFolders(), LoadFiles(), LoadKpi());
    }
}
