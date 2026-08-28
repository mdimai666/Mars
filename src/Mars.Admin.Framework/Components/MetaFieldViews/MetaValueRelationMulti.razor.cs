using Mars.Admin.Framework.Extensions;
using Flurl.Http;
using Mars.Cms.Contracts.MetaFields;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Framework.Components.MetaFieldViews;

/// <summary>
/// Мульти-значения Relation-поля: строки выбранных постов с порядком (драг),
/// добавление через пикер с мультивыбором, удаление по режиму поля.
/// </summary>
public partial class MetaValueRelationMulti
{
    [Inject] IDialogService _dialogService { get; set; } = default!;
    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] Mars.Admin.Framework.Interfaces.IMessageService _messageService { get; set; } = default!;

    [Parameter, EditorRequired] public MetaFieldEditModel Meta { get; set; } = default!;
    [CascadingParameter] public List<MetaValueEditModel> MetaValues { get; set; } = default!;

    List<MetaValueEditModel> _rows = [];
    readonly string _sortableId = "relation-multi-" + Guid.NewGuid().ToString("N");
    IReadOnlyDictionary<Guid, MetaValueRelationModelSummaryResponse> _titles = new Dictionary<Guid, MetaValueRelationModelSummaryResponse>();
    Guid[] _loadedIds = [];

    List<MetaValueEditModel> FieldRows()
        => MetaValues.Where(v => v.MetaField.Key == Meta.Key)
                     .Where(v => v.ModelId != Guid.Empty)
                     .OrderBy(v => v.Index)
                     .ToList();

    protected override void OnParametersSet()
    {
        PurgeBlankRows();
        _rows = FieldRows();
        _ = LoadTitlesAsync();
    }

    /// <summary>Пустая строка необязательного поля (не выбрано) не сохраняется — вместо неё ничего</summary>
    void PurgeBlankRows()
    {
        if (Meta.IsNullable)
            MetaValues.RemoveAll(v => v.MetaField.Key == Meta.Key && v.ModelId == Guid.Empty);
    }

    async Task LoadTitlesAsync()
    {
        var ids = _rows.Where(r => r.ModelId != Guid.Empty).Select(r => r.ModelId).Distinct().ToArray();
        if (ids.SequenceEqual(_loadedIds)) return;
        _loadedIds = ids;

        if (ids.Length == 0)
        {
            _titles = new Dictionary<Guid, MetaValueRelationModelSummaryResponse>();
            return;
        }

        try
        {
            _titles = await client.PostType.GetMetaValueRelationModels(Meta.ModelName, ids);
            StateHasChanged();
        }
        catch (FlurlHttpException ex)
        {
            _ = _messageService.Error(ex.Message);
        }
    }

    string TitleOf(MetaValueEditModel row)
        => row.ModelId == Guid.Empty
            ? "—"
            : _titles.TryGetValue(row.ModelId, out var title) ? title.Title : "…";

    string? DescriptionOf(MetaValueEditModel row)
        => _titles.TryGetValue(row.ModelId, out var title) ? title.Description : null;

    string? ImageUrlOf(MetaValueEditModel row)
        => _titles.TryGetValue(row.ModelId, out var summary) ? summary.ImageUrl : null;

    async Task AddAsync()
    {
        DialogParameters parameters = new()
        {
            Title = Meta.ModelName,
            SecondaryAction = null,
            Width = "500px",
            Modal = true,
            PreventScroll = true
        };

        var data = new MetaValueRelationSelectDialogData
        {
            ModelName = Meta.ModelName,
            ValueId = Guid.Empty,
            MultiSelect = true,
            SelectedIds = _rows.Select(r => r.ModelId).ToArray(),
        };

        IDialogReference dialog = await _dialogService.ShowDialogAsync<MetaValueRelationSelectDialog>(data, parameters);
        DialogResult? result = await dialog.Result;

        if (result.Cancelled || result.Data is not IReadOnlyCollection<Guid> ids) return;

        foreach (var id in ids)
        {
            if (_rows.Any(r => r.ModelId == id)) continue;
            _rows.Add(new MetaValueEditModel
            {
                Id = Guid.NewGuid(),
                MetaField = Meta,
                ModelId = id,
            });
        }

        SyncRows();
        await LoadTitlesAsync();
    }

    async Task RemoveAsync(MetaValueEditModel row)
    {
        var mode = MetaValueListHelper.ResolveRemoveMode(Meta);

        if (mode == MetaFieldKindCatalog.RemoveModes.DeleteConfirm)
        {
            var ok = await _dialogService.MarsDeleteConfirmation(
                "Удалить объект из системы вместе со всеми его данными?");
            if (!ok) return;

            try
            {
                await client.Post.Delete(row.ModelId);
            }
            catch (FlurlHttpException ex)
            {
                _ = _messageService.Error(ex.Message);
                return;
            }
        }

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

    /// <summary>Переиндексация строк и запись обратно в значения формы поста</summary>
    void SyncRows()
    {
        for (var i = 0; i < _rows.Count; i++) _rows[i].Index = i;

        MetaValues.RemoveAll(v => v.MetaField.Key == Meta.Key);
        MetaValues.AddRange(_rows);
    }
}
