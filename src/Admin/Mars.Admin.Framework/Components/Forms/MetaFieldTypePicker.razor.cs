using Mars.Admin.Framework.Components.MetaFieldViews;
using Mars.Admin.Framework.Extensions;
using Mars.Cms.Contracts.MetaFields;
using Mars.Forms.Contracts;
using Mars.Forms.Front;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Framework.Components.Forms;

/// <summary>
/// Пикер типа метаполя: человеческие пресеты (тип + редактор + вид поля + язык кода + кратность)
/// и сырые типы. Смена типа у сохранённого поля подтверждается диалогом — значения переносятся не все.
/// </summary>
public partial class MetaFieldTypePicker
{
    [Parameter, EditorRequired] public FormFieldDefinition Definition { get; set; } = default!;

    [Parameter] public EventCallback<FormFieldDefinition> OnSourceChanged { get; set; }

    [Inject] IDialogService DialogService { get; set; } = default!;

    [Inject] IFormEditorLocator EditorLocator { get; set; } = default!;

    /// <summary>Группы пикера типа поля: пресеты («Основные») и сырые типы («Технические»)</summary>
    static readonly IEnumerable<IGrouping<string?, MetaFieldTypePresets.PickerItem>> TypePickerGroups
        = MetaFieldTypePresets.PickerItems.GroupBy(i => i.Group);

    MetaFieldEditModel? Field => Definition.Source as MetaFieldEditModel;

    string CurrentValue => Field is null ? "" : PickerValueOf(Field);

    /// <summary>Текущий пункт пикера: подходящий пресет (тип + редактор + вид + язык кода + кратность) или «технический» тип</summary>
    static string PickerValueOf(MetaFieldEditModel field)
    {
        var preset = MetaFieldTypePresets.All.FirstOrDefault(p =>
            p.Type == field.Type
            && p.IsMultiple == field.IsMultiple
            && (string.IsNullOrEmpty(field.Editor) ? p.Editor is null : p.Editor == field.Editor)
            && (string.IsNullOrEmpty(field.Kind) ? p.Kind is null : p.Kind == field.Kind)
            && (string.IsNullOrEmpty(field.CodeLang) ? p.CodeLang is null : p.CodeLang == field.CodeLang));

        return preset is not null
            ? MetaFieldTypePresets.OptionKey(preset)
            : MetaFieldTypePresets.OptionKey(field.Type);
    }

    async Task OnChangeTypeAsync(string optionKey)
    {
        if (Field is not { } field) return;

        var preset = MetaFieldTypePresets.FindPreset(optionKey);
        var newType = preset?.Type ?? MetaFieldTypePresets.FindType(optionKey);
        if (newType is null)
        {
            StateHasChanged();
            return;
        }

        var typeChanged = newType != field.Type;
        if (typeChanged)
        {
            await ChangeTypeAsync(newType.Value, field);
            if (field.Type != newType.Value) return; // смену типа отменили
        }

        if (preset is not null)
        {
            field.Editor = preset.Editor ?? "";   // пресет задаёт редактор целиком
            field.Kind = preset.Kind ?? "";       // и вид поля (список объектов и т.п.)
            field.CodeLang = preset.CodeLang ?? ""; // и язык кода для редактора «Код»
            field.IsMultiple = preset.IsMultiple; // и кратность
        }
        else if (typeChanged && EditorLocator.GetEditorComponent(field.Editor, field.Type.ToFormFieldType(), false) is null)
        {
            field.Editor = ""; // редактор несовместим с новым типом
        }

        await OnSourceChanged.InvokeAsync(Definition);
        StateHasChanged();
    }

    async Task ChangeTypeAsync(MetaFieldType newType, MetaFieldEditModel field)
    {
        if (newType == field.Type) return;

        if (!field.IsNew)
        {
            var ok = await DialogService.MarsDeleteConfirmation(
                "Смена типа поля: текущие значения будут перенесены в новый тип, где это возможно. " +
                "Непереносимые значения будут потеряны. Продолжить?");
            if (!ok) return;
        }

        field.Type = newType;

        // правила, недоступные для нового типа, снимаем
        var available = MetaFieldValidatorCatalog.For(newType).Select(x => x.Key).ToHashSet();
        field.Validators.RemoveAll(v => !available.Contains(v.Type));

        // кратность доступна только Relation/Файл/Изображение — на прочих типах сбрасываем
        if (newType is not (MetaFieldType.Relation or MetaFieldType.File or MetaFieldType.Image))
        {
            field.IsMultiple = false;
            field.Kind = "";
        }
    }
}
