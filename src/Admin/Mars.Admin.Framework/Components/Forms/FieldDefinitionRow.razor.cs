using System.Text.Json.Nodes;
using Mars.Contracts.Resources;
using Mars.Forms.Contracts;
using Mars.Forms.Front;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;

namespace Mars.Admin.Framework.Components.Forms;

/// <summary>
/// Строка общего редактора определений — сворачиваемая карточка поля. Состав редактируемого задаёт
/// <see cref="FormDefinitionCapabilities"/>, доменные настройки типа рисуют панели из реестра
/// <see cref="IFormFieldTypeSettingsLocator"/> (у системных слотов панелей нет — только правила и редактор).
/// Общие параметры правят определение (<see cref="OnChanged"/>), панели — модель источника
/// (<see cref="OnSourceChanged"/>), поэтому источник остаётся единственным хранилищем.
/// </summary>
public partial class FieldDefinitionRow
{
    [Parameter, EditorRequired] public FormFieldDefinition Definition { get; set; } = default!;

    [Parameter] public FormDefinitionCapabilities Capabilities { get; set; } = new();

    /// <summary>Доступные типы правил; пусто — весь каталог общего слоя</summary>
    [Parameter] public IReadOnlyCollection<string>? RuleTypes { get; set; }

    /// <summary>Скоуп панелей доменных настроек типа; пусто — панелей нет</summary>
    [Parameter] public string? SettingsScope { get; set; }

    /// <summary>Компонент пикера типа поля источника (пресеты метаполей); null — тип не меняется</summary>
    [Parameter] public Type? TypePickerComponent { get; set; }

    [Parameter] public EventCallback<FormFieldDefinition> OnChanged { get; set; }

    [Parameter] public EventCallback<FormFieldDefinition> OnSourceChanged { get; set; }

    [Parameter] public EventCallback<FormFieldDefinition> OnClone { get; set; }

    [Parameter] public EventCallback<FormFieldDefinition> OnDelete { get; set; }

    [CascadingParameter] EditContext? EditContext { get; set; }

    [Inject] IFormEditorLocator EditorLocator { get; set; } = default!;

    [Inject] IFormFieldTypeSettingsLocator SettingsLocator { get; set; } = default!;

    [Inject] IStringLocalizer<AppRes> L { get; set; } = default!;

    /// <summary>Карточка раскрыта (состояние строки, не хранится)</summary>
    public bool Expanded { get; set; }

    void Toggle() => Expanded = !Expanded;

    /// <summary>Заголовок: литеральный, иначе перевод по ключу ресурса, иначе ключ поля</summary>
    string DisplayTitle
    {
        get
        {
            var title = !string.IsNullOrEmpty(Definition.Title)
                ? Definition.Title
                : string.IsNullOrEmpty(Definition.TitleKey) ? Definition.Key : L[Definition.TitleKey];

            return string.IsNullOrEmpty(title) ? "<nameless>" : title;
        }
    }

    string TitleStyle => Definition.Disabled
        ? "text-secondary text-decoration-line-through"
        : Definition.Hidden ? "fst-italic" : "";

    string TypeTitle => string.IsNullOrEmpty(Definition.TypeTitle) ? Definition.Type.ToString() : Definition.TypeTitle;

    /// <summary>Теги одной ссылкой: <see cref="InputTags2"/> сравнивает массив по ссылке, копия зациклила бы рендер</summary>
    string[] TagsValue => Definition.Tags as string[] ?? [.. Definition.Tags];

    IReadOnlyCollection<(string Key, string Title)> Editors
    {
        get
        {
            var editors = EditorLocator.EditorsFor(Definition.Type, Definition.Multiple).ToList();

            // текущий ключ показываем и без зарегистрированного компонента, иначе селект потеряет значение
            if (!string.IsNullOrEmpty(Definition.Editor) && editors.All(editor => editor.Key != Definition.Editor))
                editors.Add((Definition.Editor, Definition.Editor));

            return editors;
        }
    }

    IReadOnlyCollection<(string Key, string Title)> AvailableRules
        => Definition.RuleOptions.Count > 0
            ? Definition.RuleOptions
            : RuleTypes is { Count: > 0 } types
                ? types.Select(type => (type, type)).ToList()
                : FormRuleCatalog.All.Select(type => (type, type)).ToList();

    IReadOnlyCollection<Type> Panels
        => string.IsNullOrEmpty(SettingsScope)
            ? []
            : SettingsLocator.PanelsFor(SettingsScope, Definition.Type);

    Dictionary<string, object> PanelParameters => new()
    {
        [nameof(Definition)] = Definition,
        [nameof(OnSourceChanged)] = SourceChangedCallback,
    };

    /// <summary>
    /// Правка модели источника доменным компонентом: перечитываем определение и перерисовываем строку —
    /// параметры панели меняются по ссылке, без явной перерисовки она осталась бы со старыми значениями.
    /// </summary>
    EventCallback<FormFieldDefinition> SourceChangedCallback
        => EventCallback.Factory.Create<FormFieldDefinition>(this, OnPanelChangedAsync);

    async Task OnPanelChangedAsync(FormFieldDefinition definition)
    {
        await OnSourceChanged.InvokeAsync(definition);
        StateHasChanged();
    }

    IReadOnlyList<string> ParamsOf(string ruleType)
        => Definition.RuleParams?.TryGetValue(ruleType, out var names) == true
            ? names
            : FormRuleParams.For(ruleType);

    /// <summary>
    /// Сообщение валидации модели источника: поля проверяет валидатор графа страницы
    /// (<c>ObjectGraphDataAnnotationsValidator</c>), а рисует их общий редактор.
    /// </summary>
    string? ValidationMessage(string propertyName)
        => Definition.Source is { } source && EditContext is not null
            ? EditContext.GetValidationMessages(new FieldIdentifier(source, propertyName)).FirstOrDefault()
            : null;

    //=====================================
    // общие параметры

    Task SetTitleAsync(string value) => ChangeAsync(definition => definition.Title = value);

    Task SetKeyAsync(string value) => ChangeAsync(definition => definition.Key = value);

    Task SetDescriptionAsync(string value) => ChangeAsync(definition => definition.Description = value);

    Task SetTagsAsync(string[] tags) => ChangeAsync(definition => definition.Tags = tags);

    Task SetRequiredAsync(bool value) => ChangeAsync(definition => definition.Required = value);

    Task SetMultipleAsync(bool value) => ChangeAsync(definition => definition.Multiple = value);

    Task SetHiddenAsync(bool value) => ChangeAsync(definition => definition.Hidden = value);

    Task SetDisabledAsync(bool value) => ChangeAsync(definition => definition.Disabled = value);

    /// <summary>
    /// Прокси для числовых полей: <c>FluentNumberField</c> связывается только через <c>@bind-Value</c>,
    /// а правка определения уходит в источник общим обработчиком (паттерн <c>InputTags2</c>).
    /// </summary>
    decimal? MinValue
    {
        get => Definition.Min;
        set => _ = ChangeAsync(definition => definition.Min = value);
    }

    decimal? MaxValue
    {
        get => Definition.Max;
        set => _ = ChangeAsync(definition => definition.Max = value);
    }

    int OrderValue
    {
        get => Definition.Order;
        set => _ = ChangeAsync(definition => definition.Order = value);
    }

    Task SetEditorAsync(string editorKey)
        => ChangeAsync(definition => definition.Editor = string.IsNullOrEmpty(editorKey) ? null : editorKey);

    Task CloneAsync() => OnClone.InvokeAsync(Definition);

    Task DeleteAsync() => OnDelete.InvokeAsync(Definition);

    Task ChangeAsync(Action<FormFieldDefinition> change)
    {
        change(Definition);
        return OnChanged.InvokeAsync(Definition);
    }

    //=====================================
    // правила

    Task AddRuleAsync()
    {
        Definition.Rules.Add(new FormRuleDefinition
        {
            Type = AvailableRules.FirstOrDefault().Key is { Length: > 0 } type ? type : FormRuleCatalog.Required,
            Params = new JsonObject(),
        });

        return OnChanged.InvokeAsync(Definition);
    }

    Task RemoveRuleAsync(int index)
    {
        if (index < 0 || index >= Definition.Rules.Count) return Task.CompletedTask;

        Definition.Rules.RemoveAt(index);
        return OnChanged.InvokeAsync(Definition);
    }

    Task SetRuleTypeAsync(int index, string type)
    {
        if (index < 0 || index >= Definition.Rules.Count || string.IsNullOrEmpty(type)) return Task.CompletedTask;

        var rule = Definition.Rules[index];
        if (rule.Type == type) return Task.CompletedTask;

        // параметры у правил свои: переносим только сообщение
        var message = FormRuleParams.Read(rule, "message");
        Definition.Rules[index] = rule with
        {
            Type = type,
            Params = message is null ? new JsonObject() : new JsonObject { ["message"] = message },
        };

        return OnChanged.InvokeAsync(Definition);
    }

    Task SetRuleParamAsync(int index, string name, string? value)
    {
        if (index < 0 || index >= Definition.Rules.Count) return Task.CompletedTask;

        var rule = Definition.Rules[index];
        var parameters = rule.Params is JsonObject existing ? (JsonObject)existing.DeepClone() : new JsonObject();

        if (string.IsNullOrWhiteSpace(value)) parameters.Remove(name);
        else parameters[name] = value;

        Definition.Rules[index] = rule with { Params = parameters };
        return OnChanged.InvokeAsync(Definition);
    }
}
