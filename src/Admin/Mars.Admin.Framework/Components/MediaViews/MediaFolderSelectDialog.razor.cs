using Mars.Media.Contracts.Files;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Framework.Components.MediaViews;

/// <summary>
/// Пикер папки медиа: выбор папки для загрузки (например, в настройках мета-поля
/// Файл/Изображение). Результат — путь папки (<see cref="FolderResponse.Path"/>);
/// «По умолчанию» возвращает пустую строку (папка года).
/// </summary>
public partial class MediaFolderSelectDialog
{
    [Inject] IMarsWebApiClient client { get; set; } = default!;

    [CascadingParameter] public FluentDialog Dialog { get; set; } = default!;

    /// <summary>Текущий путь (для подписи; диалог всегда открывается с корня)</summary>
    [Parameter] public string Content { get; set; } = "";

    Guid? _parentId;
    List<FolderResponse> _folders = [];
    List<FolderResponse> _breadcrumbs = [];
    bool _loading;

    string CurrentPath => _breadcrumbs.Count > 0 ? _breadcrumbs[^1].Path : "Media";

    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
    }

    async Task ReloadAsync()
    {
        _loading = true;
        StateHasChanged();

        _folders = await client.Media.ListFolders(_parentId);
        _breadcrumbs = _parentId is Guid folderId
            ? await client.Media.FolderBreadcrumbs(folderId)
            : [];

        _loading = false;
        StateHasChanged();
    }

    async Task OpenFolder(FolderResponse folder)
    {
        _parentId = folder.Id;
        await ReloadAsync();
    }

    async Task GoUp()
    {
        _parentId = _breadcrumbs.Count > 1 ? _breadcrumbs[^2].Id : null;
        await ReloadAsync();
    }

    Task SelectRoot() => Dialog.CloseAsync("");

    Task Select(FolderResponse folder) => Dialog.CloseAsync(folder.Path);

    Task CancelAsync() => Dialog.CancelAsync();
}
