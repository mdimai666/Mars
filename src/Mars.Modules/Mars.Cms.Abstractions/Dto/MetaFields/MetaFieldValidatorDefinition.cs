using System.Text.Json.Nodes;

namespace Mars.Cms.Abstractions.Dto.MetaFields;

/// <summary>
/// Правило валидации значения мета-поля; хранится в массиве <c>Options.validators</c>.
/// Тип — дискриминатор из реестра <c>Mars.Cms.Abstractions.Utils.MetaFieldValueValidators</c>.
/// </summary>
public record MetaFieldValidatorDefinition
{
    public required string Type { get; init; }

    /// <summary>Параметры валидатора (pattern/message/min/max)</summary>
    public JsonObject? Params { get; init; }

    public static IReadOnlyList<MetaFieldValidatorDefinition> FromOptions(JsonNode? options)
    {
        if (options is not JsonObject obj || obj["validators"] is not JsonArray array)
            return [];

        var result = new List<MetaFieldValidatorDefinition>();
        foreach (var node in array)
        {
            if (node is not JsonObject item) continue;

            var type = item["type"] is JsonValue value && value.TryGetValue<string>(out var t) ? t : null;
            if (string.IsNullOrEmpty(type)) continue;

            result.Add(new MetaFieldValidatorDefinition
            {
                Type = type,
                Params = item["params"] as JsonObject,
            });
        }

        return result;
    }
}
