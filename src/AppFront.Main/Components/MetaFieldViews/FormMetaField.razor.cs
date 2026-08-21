using AppFront.Shared.Extensions;
using Mars.Core.Features;
using Mars.Shared.Contracts.MetaFields;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace AppFront.Shared.Components.MetaFieldViews;

public partial class FormMetaField
{
    [CascadingParameter]
    public List<MetaFieldEditModel> Model { get; set; } = default!;

    [CascadingParameter]
    public IReadOnlyCollection<MetaRelationModelResponse> MetaRelationModels { get; set; } = default!;

    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] IDialogService _dialogService { get; set; } = default!;

    void OnChangeFieldTitle(string value, MetaFieldEditModel model)
    {
        model.Title = value;
        if (string.IsNullOrWhiteSpace(model.Key))
        {
            model.Key = TextTool.TranslateToPostSlug(model.Title);
        }
    }

    async Task OnChangeFieldType(MetaFieldType newType, MetaFieldEditModel field)
    {
        if (newType == field.Type) return;

        if (!field.IsNew)
        {
            var ok = await _dialogService.MarsDeleteConfirmation(
                "Смена типа поля: текущие значения будут перенесены в новый тип, где это возможно. " +
                "Непереносимые значения будут потеряны. Продолжить?");
            if (!ok)
            {
                UpdateState();
                return;
            }
        }

        field.Type = newType;
    }

    void OnClone(MetaFieldEditModel field)
    {
        Model.Add(field.Clone(Model.Count));
    }

    public void UpdateState()
    {
        StateHasChanged();
    }

    void OnDelete(MetaFieldEditModel field)
    {
        Model.Remove(field);
    }

    public static MetaFieldEditModel NewField(int order)
    {
        return new MetaFieldEditModel
        {
            Id = Guid.NewGuid(),
            Order = order,
        };
    }
}
