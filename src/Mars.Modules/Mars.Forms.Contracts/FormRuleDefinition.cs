using System.Text.Json.Nodes;

namespace Mars.Forms.Contracts;

/// <summary>
/// Правило валидации значения поля формы; хранится в массиве <c>rules</c> элемента формы.
/// Тип — дискриминатор из реестра <c>Mars.Forms.Abstractions.Validation.IFormRuleRegistry</c>.
/// Форма правила совпадает с <c>MetaFieldValidatorDefinition</c> — правила метаполей
/// переносятся в общий реестр без изменения параметров.
/// </summary>
public record FormRuleDefinition
{
    public required string Type { get; init; }

    /// <summary>Параметры правила (pattern/message/min/max/…)</summary>
    public JsonObject? Params { get; init; }

    /// <summary>Разбирает массив <c>[{type, params}]</c>; битые элементы пропускаются</summary>
    public static IReadOnlyList<FormRuleDefinition> FromJson(JsonNode? node)
    {
        if (node is not JsonArray array) return [];

        var result = new List<FormRuleDefinition>();
        foreach (var item in array)
        {
            if (item is not JsonObject obj) continue;

            var type = obj["type"] is JsonValue value && value.TryGetValue<string>(out var t) ? t : null;
            if (string.IsNullOrEmpty(type)) continue;

            result.Add(new FormRuleDefinition
            {
                Type = type,
                Params = obj["params"] as JsonObject,
            });
        }

        return result;
    }

    public static JsonNode? ToJson(IReadOnlyCollection<FormRuleDefinition>? rules)
    {
        if (rules is null || rules.Count == 0) return null;

        var array = new JsonArray();
        foreach (var rule in rules)
        {
            array.Add(new JsonObject
            {
                ["type"] = rule.Type,
                ["params"] = rule.Params?.DeepClone(),
            });
        }

        return array;
    }
}

/// <summary>Дискриминаторы правил валидации формы</summary>
public static class FormRuleCatalog
{
    public const string Required = "required";
    public const string Regex = "regex";
    public const string Length = "length";
    public const string Min = "min";
    public const string Max = "max";

    /// <summary>Требует данных владельца — реализуется в скоупе провайдера, не встроенное</summary>
    public const string Unique = "unique";

    /// <summary>Встроенные правила общего слоя: чистые, без доступа к данным</summary>
    public static readonly IReadOnlyList<string> BuiltIn = [Required, Regex, Length, Min, Max];

    public static readonly IReadOnlyList<string> All = [Required, Regex, Length, Min, Max, Unique];
}
