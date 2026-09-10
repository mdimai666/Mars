using Flurl.Http;
using Mars.Cms.Contracts.MetaFields;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Framework.Components.MetaFieldViews;

/// <summary>
/// Одинарное значение Relation-поля: плитка выбранного объекта (заголовок + миниатюра),
/// выбор через пикер в одиночном режиме, очистка.
/// </summary>
public partial class MetaValueRelationSingle
{
    [Inject] IDialogService _dialogService { get; set; } = default!;
    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] Mars.Admin.Framework.Interfaces.IMessageService _messageService { get; set; } = default!;

    [Parameter, EditorRequired] public MetaFieldEditModel Meta { get; set; } = default!;
    [CascadingParameter] public List<MetaValueEditModel> MetaValues { get; set; } = default!;

    bool _busy;
    MetaValueRelationModelSummaryResponse? _model;
    Guid _loadedId = Guid.Empty;

    Guid SelectedId => MetaValues.FirstOrDefault(v => v.MetaField.Key == Meta.Key && v.ModelId != Guid.Empty)?.ModelId ?? Guid.Empty;

    protected override void OnParametersSet()
    {
        _ = LoadAsync();
    }

    async Task LoadAsync()
    {
        var id = SelectedId;
        if (id == _loadedId) return;

        _loadedId = id;
        if (id == Guid.Empty)
        {
            _model = null;
            return;
        }

        _busy = true;
        StateHasChanged();

        try
        {
            var models = await client.PostType.GetMetaValueRelationModels(Meta.ModelName, [id]);
            _model = models.GetValueOrDefault(id);
        }
        catch (FlurlHttpException ex)
        {
            _ = _messageService.Error(ex.Message);
        }
        finally
        {
            _busy = false;
            StateHasChanged();
        }
    }

    async Task SelectAsync()
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
            ValueId = SelectedId,
        };

        IDialogReference dialog = await _dialogService.ShowDialogAsync<MetaValueRelationSelectDialog>(data, parameters);
        DialogResult? result = await dialog.Result;

        if (result.Cancelled || result.Data is not MetaValueRelationModelSummaryResponse selected) return;

        SetValue(selected.Id);
        _model = selected;
        _loadedId = selected.Id;
        StateHasChanged();
    }

    void ClearAsync()
    {
        SetValue(Guid.Empty);
        _model = null;
        _loadedId = Guid.Empty;

        // пустая строка необязательного поля (не выбрано) не сохраняется — вместо неё ничего
        if (Meta.IsNullable)
            MetaValues.RemoveAll(v => v.MetaField.Key == Meta.Key && v.ModelId == Guid.Empty);

        StateHasChanged();
    }

    void SetValue(Guid modelId)
    {
        var row = MetaValues.FirstOrDefault(v => v.MetaField.Key == Meta.Key);
        if (row is null)
        {
            MetaValues.Add(new MetaValueEditModel
            {
                Id = Guid.NewGuid(),
                Index = 0,
                MetaField = Meta,
                ModelId = modelId,
            });
            return;
        }

        row.ModelId = modelId;
        row.Index = 0;
    }
}
