using System.Text.Json.Nodes;
using Mars.Contracts.Resources;
using Mars.Forms.Contracts;
using Mars.Forms.Front;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Mars.Admin.Framework.Components.Forms;

/// <summary>
/// Строка общего редактора определений — сворачиваемая карточка поля. Состав редактируемого задаёт
/// <see cref="FormDefinitionCapabilities"/>: у системных слотов это правила и редактор, а тип, ключ,
/// заголовок и обязательность приходят из каталога источника и показаны только для чтения.
/// </summary>
public partial class FieldDefinitionRow
{
    [Parameter, EditorRequired] public FormFieldDefinition Definition { get; set; } = default!;

    [Parameter] public FormDefinitionCapabilities Capabilities { get; set; } = new();

    /// <summary>Доступные типы правил; пусто — весь каталог общего слоя</summary>
    [Parameter] public IReadOnlyCollection<string>? RuleTypes { get; set; }

    [Parameter] public EventCallback<FormFieldDefinition> OnChanged { get; set; }

    [Inject] IFormEditorLocator EditorLocator { get; set; } = default!;

    [Inject] IStringLocalizer<AppRes> L { get; set; } = default!;

    /// <summary>Карточка раскрыта (состояние строки, не хранится)</summary>
    public bool Expanded { get; set; }

    void Toggle() => Expanded = !Expanded;

    /// <summary>Заголовок: литеральный, иначе перевод по ключу ресурса, иначе ключ поля</summary>
    string DisplayTitle
        => !string.IsNullOrEmpty(Definition.Title)
            ? Definition.Title
            : string.IsNullOrEmpty(Definition.TitleKey) ? Definition.Key : L[Definition.TitleKey];

    IReadOnlyCollection<string> AvailableRuleTypes
        => RuleTypes is { Count: > 0 } types ? types.ToList() : FormRuleCatalog.All;

    /// <summary>Совместимые с полем редакторы из реестра фронта</summary>
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

    Task SetEditorAsync(string editorKey)
    {
        Definition.Editor = string.IsNullOrEmpty(editorKey) ? null : editorKey;
        return OnChanged.InvokeAsync(Definition);
    }

    Task AddRuleAsync()
    {
        Definition.Rules.Add(new FormRuleDefinition
        {
            Type = AvailableRuleTypes.FirstOrDefault() ?? FormRuleCatalog.Required,
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
