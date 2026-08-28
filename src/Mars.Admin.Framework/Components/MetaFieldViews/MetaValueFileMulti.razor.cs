using Mars.Admin.Framework.Services;
using Mars.Media.Contracts.Files;
using Mars.Cms.Contracts.MetaFields;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Framework.Components.MetaFieldViews;

/// <summary>
/// Множественные значения поля Файл/Изображение: плитки с превью (каждая запрашивает файл сама),
/// порядок драгом, добавление дроп-зоной и мультивыбором из медиа, удаление = отвязка
/// (файл остаётся в медиа).
/// </summary>
public partial class MetaValueFileMulti
{
    [Inject] IAppMediaService _mediaService { get; set; } = default!;

    [Parameter, EditorRequired] public MetaFieldEditModel Meta { get; set; } = default!;
    [CascadingParameter] public List<MetaValueEditModel> MetaValues { get; set; } = default!;

    List<MetaValueEditModel> _rows = [];
    readonly string _sortableId = "file-multi-" + Guid.NewGuid().ToString("N");
    readonly Dictionary<Guid, FileSummaryResponse?> _previews = [];
    readonly HashSet<Guid> _loadingIds = [];

    /// <summary>Вид отображения (Options.viewMode): таблица (дефолт) или карточки</summary>
    bool IsCardsView => Meta.ViewMode == MetaFieldKindCatalog.ViewModes.Cards;

    List<MetaValueEditModel> FieldRows()
        => MetaValues.Where(v => v.MetaField.Key == Meta.Key)
                     .Where(v => v.ModelId != Guid.Empty)
                     .OrderBy(v => v.Index)
                     .ToList();

    FileSummaryResponse? PreviewOf(MetaValueEditModel row)
        => _previews.GetValueOrDefault(row.ModelId);

    bool IsLoading(MetaValueEditModel row)
        => _loadingIds.Contains(row.ModelId);

    protected override void OnParametersSet()
    {
        PurgeBlankRows();
        _rows = FieldRows();
        _ = LoadPreviewsAsync();
    }

    /// <summary>Пустая строка необязательного поля (не выбрано) не сохраняется — вместо неё ничего</summary>
    void PurgeBlankRows()
    {
        if (Meta.IsNullable)
            MetaValues.RemoveAll(v => v.MetaField.Key == Meta.Key && v.ModelId == Guid.Empty);
    }

    /// <summary>Превью каждого файла запрашивается отдельно (как в одинарных плитках)</summary>
    async Task LoadPreviewsAsync()
    {
        foreach (var id in _rows.Select(r => r.ModelId).Distinct())
        {
            if (_previews.ContainsKey(id) || _loadingIds.Contains(id)) continue;

            _loadingIds.Add(id);
            _ = LoadPreviewAsync(id);
        }

        // превью могли устареть (файл отвязали) — лишние не держим
        var actual = _rows.Select(r => r.ModelId).ToHashSet();
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
            if (_rows.Any(r => r.ModelId == file.Id)) continue;
            _rows.Add(new MetaValueEditModel
            {
                Id = Guid.NewGuid(),
                MetaField = Meta,
                ModelId = file.Id,
            });
            _previews[file.Id] = file;
        }

        SyncRows();
    }

    /// <summary>Дроп-зона: файлы грузятся в медиа и становятся значениями (без постов)</summary>
    Task OnFilesUploadedAsync(IReadOnlyCollection<FileDetailResponse> files)
    {
        foreach (var file in files)
        {
            _rows.Add(new MetaValueEditModel
            {
                Id = Guid.NewGuid(),
                MetaField = Meta,
                ModelId = file.Id,
            });
            _previews[file.Id] = file;
        }

        SyncRows();
        return Task.CompletedTask;
    }

    /// <summary>Удаление = отвязка значения (файл остаётся в медиа)</summary>
    void RemoveAsync(MetaValueEditModel row)
    {
        _rows.Remove(row);
        SyncRows();
    }

    void OnSort(FluentSortableListEventArgs args)
    {
        if (args is null || args.OldIndex == args.NewIndex) return;

        var item = _rows[args.OldIndex];
        _rows.RemoveAt(args.OldIndex);
        _rows.Insert(args.NewIndex, item);
        SyncRows();
    }

    /// <summary>Переиндексация строк и запись обратно в значения формы</summary>
    void SyncRows()
    {
        for (var i = 0; i < _rows.Count; i++) _rows[i].Index = i;

        MetaValues.RemoveAll(v => v.MetaField.Key == Meta.Key);
        MetaValues.AddRange(_rows);
        StateHasChanged();
    }
}
