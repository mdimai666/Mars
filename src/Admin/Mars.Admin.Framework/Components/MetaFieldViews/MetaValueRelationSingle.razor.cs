using Flurl.Http;
using Mars.Admin.Framework.Components.Forms;
using Mars.Cms.Contracts.MetaFields;
using Mars.Forms.Front;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Framework.Components.MetaFieldViews;

/// <summary>
/// Одинарное значение Relation-поля: плитка выбранного объекта (заголовок + миниатюра),
/// выбор через пикер в одиночном режиме, очистка. Значение — идентификатор из привязки поля,
/// «не выбрано» пишется как null (строку-заглушку стор не хранит).
/// </summary>
public partial class MetaValueRelationSingle
{
    [Inject] IDialogService _dialogService { get; set; } = default!;
    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] Mars.Admin.Framework.Interfaces.IMessageService _messageService { get; set; } = default!;

    [Parameter, EditorRequired] public FormFieldBinding Binding { get; set; } = default!;

    bool _busy;
    MetaValueRelationModelSummaryResponse? _model;
    Guid _loadedId = Guid.Empty;

    /// <summary>Цель пикера (ключ реестра моделей связей) из дескриптора поля</summary>
    string ModelName => Binding.Field.ModelName ?? "";

    Guid SelectedId => Binding.Value is Guid id ? id : Guid.Empty;

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
            var models = await client.PostType.GetMetaValueRelationModels(ModelName, [id]);
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
            Title = ModelName,
            SecondaryAction = null,
            Width = "500px",
            Modal = true,
            PreventScroll = true
        };

        var data = new MetaValueRelationSelectDialogData
        {
            ModelName = ModelName,
            ValueId = SelectedId,
        };

        IDialogReference dialog = await _dialogService.ShowDialogAsync<MetaValueRelationSelectDialog>(data, parameters);
        DialogResult? result = await dialog.Result;

        if (result.Cancelled || result.Data is not MetaValueRelationModelSummaryResponse selected) return;

        Binding.Value = selected.Id;
        _model = selected;
        _loadedId = selected.Id;
        StateHasChanged();
    }

    void ClearAsync()
    {
        Binding.Value = null;
        _model = null;
        _loadedId = Guid.Empty;

        StateHasChanged();
    }
}
