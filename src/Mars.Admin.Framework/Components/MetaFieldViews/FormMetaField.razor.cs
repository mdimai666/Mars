using Mars.Admin.Framework.Components.MediaViews;
using Mars.Admin.Framework.Extensions;
using Mars.Cms.Contracts.MetaFields;
using Mars.Core.Features;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Framework.Components.MetaFieldViews;

public partial class FormMetaField
{
    [CascadingParameter]
    public List<MetaFieldEditModel> Model { get; set; } = default!;

    [CascadingParameter]
    public IReadOnlyCollection<MetaRelationModelResponse> MetaRelationModels { get; set; } = default!;

    /// <summary>Ключ поля, на который сейчас указывает фича типа (бейдж, запрет
    /// удаления и смены типа); пусто — таких полей нет</summary>
    [Parameter] public string? FeatureFieldKey { get; set; }

    /// <summary>Фича «Контент» включена: поле с фиксированным ключом content
    /// защищено (бейдж, запрет удаления, смены типа и переименования)</summary>
    [Parameter] public bool ContentFeatureEnabled { get; set; }

    /// <summary>Поле переименовано (старый ключ, новый ключ) — страница двигает указатель фичи</summary>
    [Parameter] public Action<string, string>? OnFieldKeyRenamed { get; set; }

    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] IDialogService _dialogService { get; set; } = default!;
    [Inject] IMetaFieldEditorLocator _editorLocator { get; set; } = default!;

    /// <summary>Поле — текущий указатель фичи типа (например, картинки поста)</summary>
    bool IsFeatureField(MetaFieldEditModel field)
        => FeatureFieldKey is not null && field.Key == FeatureFieldKey;

    /// <summary>Поле контента включённой фичи «Контент» (фиксированный ключ)</summary>
    bool IsContentFeatureField(MetaFieldEditModel field)
        => ContentFeatureEnabled && field.Key == FeatureFieldsCatalog.ContentFieldKey;

    /// <summary>Поле защищено фичей: нельзя удалить, сменить тип (и ключ для контента)</summary>
    bool IsProtectedFeatureField(MetaFieldEditModel field)
        => IsFeatureField(field) || IsContentFeatureField(field);

    void OnChangeFieldTitle(string value, MetaFieldEditModel model)
    {
        model.Title = value;
        if (string.IsNullOrWhiteSpace(model.Key))
        {
            model.Key = TextTool.TranslateToPostSlug(model.Title);
        }
    }

    void OnChangeFieldKey(string value, MetaFieldEditModel field)
    {
        var oldKey = field.Key;
        field.Key = value;
        if (oldKey != value)
            OnFieldKeyRenamed?.Invoke(oldKey, value);
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

    /// <summary>Тумблер кратности: выключение снимает вид поля (список объектов недоступен без множественности)</summary>
    void OnToggleIsMultiple(bool value, MetaFieldEditModel field)
    {
        field.IsMultiple = value;
        if (!value) field.Kind = "";
    }

    /// <summary>Правила валидации, доступные типу поля</summary>
    IReadOnlyCollection<(string Key, string Title)> AvailableValidators(MetaFieldEditModel field)
        => MetaFieldValidatorCatalog.For(field.Type);

    void AddValidatorRule(MetaFieldEditModel field)
    {
        var available = AvailableValidators(field);
        if (available.Count == 0) return;

        field.Validators.Add(new MetaFieldEditModel.MetaFieldValidatorEditRow { Type = available.First().Key });
    }

    /// <summary>Группы пикера типа поля: пресеты («Основные») и сырые типы («Технические»)</summary>
    static readonly IEnumerable<IGrouping<string?, MetaFieldTypePresets.PickerItem>> TypePickerGroups
        = MetaFieldTypePresets.PickerItems.GroupBy(i => i.Group);

    /// <summary>Текущий пункт пикера типа: подходящий пресет (тип + редактор + вид + язык кода + кратность) или «технический» тип</summary>
    string GetTypePickerValue(MetaFieldEditModel field)
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
        {
            field.Editor = preset.Editor ?? ""; // пресет задаёт редактор целиком
            field.Kind = preset.Kind ?? ""; // и вид поля (список объектов и т.п.)
            field.CodeLang = preset.CodeLang ?? ""; // и язык кода для редактора «Код»
            field.IsMultiple = preset.IsMultiple; // и кратность
        }
        else if (typeChanged && _editorLocator.GetEditorComponent(field.Editor, field.Type) is null)
            field.Editor = ""; // редактор несовместим с новым типом

        UpdateState();
    }

    void OnClone(MetaFieldEditModel field)
    {
        Model.Add(field.Clone(Model.Count));
    }

    /// <summary>Выбор папки загрузки для полей Файл/Изображение</summary>
    async Task OpenFolderPickerAsync(MetaFieldEditModel field)
    {
        DialogParameters parameters = new()
        {
            Title = "Папка загрузки",
            SecondaryAction = null,
            Width = "500px",
            Modal = true,
            PreventScroll = true
        };

        IDialogReference dialog = await _dialogService.ShowDialogAsync<MediaFolderSelectDialog>("", parameters);
        DialogResult? result = await dialog.Result;

        if (result.Cancelled || result.Data is not string path) return;

        field.UploadFolder = path;
        UpdateState();
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
