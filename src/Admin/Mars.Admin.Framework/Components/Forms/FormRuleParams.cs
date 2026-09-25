using System.Text.Json.Nodes;
using Mars.Forms.Contracts;

namespace Mars.Admin.Framework.Components.Forms;

/// <summary>
/// Параметры правил валидации в редакторах формы: какие поля показывать для типа правила
/// (значения хранятся строками в <see cref="FormRuleDefinition.Params"/>).
/// </summary>
public static class FormRuleParams
{
    public static IReadOnlyList<string> For(string ruleType) => ruleType switch
    {
        FormRuleCatalog.Regex => ["pattern", "message"],
        FormRuleCatalog.Length => ["min", "max", "message"],
        FormRuleCatalog.Min or FormRuleCatalog.Max => ["value", "message"],
        _ => ["message"],
    };

    public static string? Read(FormRuleDefinition rule, string name)
        => rule.Params?[name] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
}
