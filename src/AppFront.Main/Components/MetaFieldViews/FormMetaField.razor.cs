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
    [Inject] IMetaFieldEditorLocator _editorLocator { get; set; } = default!;

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

    /// <summary>Группы пикера типа поля: пресеты («Основные») и сырые типы («Технические»)</summary>
    static readonly IEnumerable<IGrouping<string?, MetaFieldTypePresets.PickerItem>> TypePickerGroups
        = MetaFieldTypePresets.PickerItems.GroupBy(i => i.Group);

    /// <summary>Текущий пункт пикера типа: подходящий пресет (тип + редактор) или «технический» тип</summary>
    string GetTypePickerValue(MetaFieldEditModel field)
    {
        var preset = MetaFieldTypePresets.All.FirstOrDefault(p =>
            p.Type == field.Type
            && (string.IsNullOrEmpty(field.Editor) ? p.Editor is null : p.Editor == field.Editor));

        return preset is not null
            ? MetaFieldTypePresets.OptionKey(preset)
            : MetaFieldTypePresets.OptionKey(field.Type);
    }

    async Task OnChangeFieldTypePicker(string optionKey, MetaFieldEditModel field)
    {
        var preset = MetaFieldTypePresets.FindPreset(optionKey);
        var newType = preset?.Type ?? MetaFieldTypePresets.FindType(optionKey);
        if (newType is null)
        {
            UpdateState();
            return;
        }

        var typeChanged = newType != field.Type;
        if (typeChanged)
        {
            await OnChangeFieldType(newType.Value, field);
            if (field.Type != newType.Value) return; // смену типа отменили
        }

        if (preset is not null)
            field.Editor = preset.Editor ?? ""; // пресет задаёт редактор целиком
        else if (typeChanged && _editorLocator.GetEditorComponent(field.Editor, field.Type) is null)
            field.Editor = ""; // редактор несовместим с новым типом

        UpdateState();
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
