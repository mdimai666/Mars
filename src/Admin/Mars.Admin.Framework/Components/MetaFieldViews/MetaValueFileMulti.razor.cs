using Mars.Admin.Framework.Components.Forms;
using Mars.Admin.Framework.Services;
using Mars.Cms.Contracts.MetaFields;
using Mars.Forms.Contracts;
using Mars.Forms.Front;
using Mars.Media.Contracts.Files;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Framework.Components.MetaFieldViews;

/// <summary>
/// Множественные значения поля Файл/Изображение: плитки с превью (каждая запрашивает файл сама),
/// порядок драгом, добавление дроп-зоной и мультивыбором из медиа, удаление = отвязка
/// (файл остаётся в медиа). Значения — список идентификаторов файлов из привязки поля.
/// </summary>
public partial class MetaValueFileMulti
{
    [Inject] IAppMediaService _mediaService { get; set; } = default!;

    [Parameter, EditorRequired] public FormFieldBinding Binding { get; set; } = default!;

    List<Guid> _rows = [];
    readonly string _sortableId = "file-multi-" + Guid.NewGuid().ToString("N");
    readonly Dictionary<Guid, FileSummaryResponse?> _previews = [];
    readonly HashSet<Guid> _loadingIds = [];

    /// <summary>Вид отображения (Options.viewMode): таблица (дефолт) или карточки</summary>
    bool IsCardsView => Binding.Field.Options.GetViewMode() == MetaFieldKindCatalog.ViewModes.Cards;

    /// <summary>Папка загрузки дропа (пусто = папка года)</summary>
    string UploadFolder => Binding.Field.Options.GetUploadFolder();

    /// <summary>Дроп-зона загрузки включена (отсутствие параметра = включена)</summary>
    bool DropZoneEnabled => Binding.Field.Options.IsDropZoneEnabled();

    bool IsImage => Binding.Field.Type == FormFieldType.Image;

    List<Guid> SelectedIds()
        => Binding.List.OfType<Guid>().Where(id => id != Guid.Empty).ToList();

    FileSummaryResponse? PreviewOf(Guid id)
        => _previews.GetValueOrDefault(id);

    bool IsLoading(Guid id)
        => _loadingIds.Contains(id);

    protected override void OnParametersSet()
    {
        _rows = SelectedIds();
        _ = LoadPreviewsAsync();
    }

    /// <summary>Превью каждого файла запрашивается отдельно (как в одинарных плитках)</summary>
    async Task LoadPreviewsAsync()
    {
        foreach (var id in _rows)
        {
            if (_previews.ContainsKey(id) || _loadingIds.Contains(id)) continue;

            _loadingIds.Add(id);
            _ = LoadPreviewAsync(id);
        }

        // превью могли устареть (файл отвязали) — лишние не держим
        var actual = _rows.ToHashSet();
        foreach (var key in _previews.Keys.Where(k => !actual.Contains(k)).ToList())
            _previews.Remove(key);
    }

    async Task LoadPreviewAsync(Guid id)
    {
        try
        {
            _previews[id] = await _mediaService.Get(id);
        }
        finally
        {
            _loadingIds.Remove(id);
            await InvokeAsync(StateHasChanged);
        }
    }

    async Task AddFromMediaAsync()
    {
        var files = await _mediaService.OpenSelectMediaMany();
        if (files.Count == 0) return;

        foreach (var file in files)
        {
            if (_rows.Contains(file.Id)) continue;
            _rows.Add(file.Id);
            _previews[file.Id] = file;
        }

        SyncValues();
    }

    /// <summary>Дроп-зона: файлы грузятся в медиа и становятся значениями (без постов)</summary>
    Task OnFilesUploadedAsync(IReadOnlyCollection<FileDetailResponse> files)
    {
        foreach (var file in files)
        {
            _rows.Add(file.Id);
            _previews[file.Id] = file;
        }

        SyncValues();
        return Task.CompletedTask;
    }

    /// <summary>Удаление = отвязка значения (файл остаётся в медиа)</summary>
    void RemoveAsync(Guid id)
    {
        _rows.Remove(id);
        SyncValues();
    }

    void OnSort(FluentSortableListEventArgs args)
    {
        if (args is null || args.OldIndex == args.NewIndex) return;

        var item = _rows[args.OldIndex];
        _rows.RemoveAt(args.OldIndex);
        _rows.Insert(args.NewIndex, item);
        SyncValues();
    }

    /// <summary>Записать порядок значений в привязку поля</summary>
    void SyncValues()
    {
        Binding.Values.SetList(Binding.Field, _rows.Cast<object?>().ToList());
        StateHasChanged();
    }
}
