using Flurl.Http;
using Mars.Admin.Framework.Components.Forms;
using Mars.Admin.Framework.Extensions;
using Mars.Cms.Contracts.MetaFields;
using Mars.Forms.Front;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Framework.Components.MetaFieldViews;

/// <summary>
/// Мульти-значения Relation-поля: строки выбранных постов с порядком (драг),
/// добавление через пикер с мультивыбором, удаление по режиму поля.
/// Значения — список идентификаторов из привязки поля (порядок списка = порядок значений).
/// </summary>
public partial class MetaValueRelationMulti
{
    [Inject] IDialogService _dialogService { get; set; } = default!;
    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] Mars.Admin.Framework.Interfaces.IMessageService _messageService { get; set; } = default!;

    [Parameter, EditorRequired] public FormFieldBinding Binding { get; set; } = default!;

    /// <summary>Цель пикера (ключ реестра моделей связей) из дескриптора поля</summary>
    string ModelName => Binding.Field.ModelName ?? "";

    List<Guid> _rows = [];
    readonly string _sortableId = "relation-multi-" + Guid.NewGuid().ToString("N");
    IReadOnlyDictionary<Guid, MetaValueRelationModelSummaryResponse> _titles = new Dictionary<Guid, MetaValueRelationModelSummaryResponse>();
    Guid[] _loadedIds = [];

    List<Guid> SelectedIds()
        => Binding.List.OfType<Guid>().Where(id => id != Guid.Empty).ToList();

    protected override void OnParametersSet()
    {
        _rows = SelectedIds();
        _ = LoadTitlesAsync();
    }

    async Task LoadTitlesAsync()
    {
        var ids = _rows.Distinct().ToArray();
        if (ids.SequenceEqual(_loadedIds)) return;
        _loadedIds = ids;

        if (ids.Length == 0)
        {
            _titles = new Dictionary<Guid, MetaValueRelationModelSummaryResponse>();
            return;
        }

        try
        {
            _titles = await client.PostType.GetMetaValueRelationModels(ModelName, ids);
            StateHasChanged();
        }
        catch (FlurlHttpException ex)
        {
            _ = _messageService.Error(ex.Message);
        }
    }

    string TitleOf(Guid id)
        => id == Guid.Empty
            ? "—"
            : _titles.TryGetValue(id, out var title) ? title.Title : "…";

    string? DescriptionOf(Guid id)
        => _titles.TryGetValue(id, out var title) ? title.Description : null;

    string? ImageUrlOf(Guid id)
        => _titles.TryGetValue(id, out var summary) ? summary.ImageUrl : null;

    async Task AddAsync()
    {
        DialogParameters parameters = new()
        {
            Title = ModelName,
            SecondaryAction = null,
            Width = "500px",
            Modal = true,
            PreventScroll = true
        };

        var data = new MetaValueRelationSelectDialogData
        {
            ModelName = ModelName,
            ValueId = Guid.Empty,
            MultiSelect = true,
            SelectedIds = _rows.ToArray(),
        };

        IDialogReference dialog = await _dialogService.ShowDialogAsync<MetaValueRelationSelectDialog>(data, parameters);
        DialogResult? result = await dialog.Result;

        if (result.Cancelled || result.Data is not IReadOnlyCollection<Guid> ids) return;

        foreach (var id in ids)
        {
            if (_rows.Contains(id)) continue;
            _rows.Add(id);
        }

        SyncValues();
        await LoadTitlesAsync();
    }

    async Task RemoveAsync(Guid id)
    {
        if (MetaValueListHelper.ResolveRemoveMode(Binding.Field) == MetaFieldKindCatalog.RemoveModes.DeleteConfirm)
        {
            var ok = await _dialogService.MarsDeleteConfirmation(
                "Удалить объект из системы вместе со всеми его данными?");
            if (!ok) return;

            try
            {
                await client.Post.Delete(id);
            }
            catch (FlurlHttpException ex)
            {
                _ = _messageService.Error(ex.Message);
                return;
            }
        }

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
    void SyncValues() => Binding.Values.SetList(Binding.Field, _rows.Cast<object?>().ToList());
}
