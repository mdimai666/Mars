using System.Text.Json.Nodes;

namespace Mars.Cms.Abstractions.Dto.MetaFields;

/// <summary>
/// Настройка генератора значения мета-поля; хранится в <c>Options.generator</c> (один генератор на поле).
/// Тип — дискриминатор из каталога <c>Mars.Contracts.MetaFields.MetaFieldGeneratorCatalog</c>.
/// </summary>
public record MetaFieldGeneratorDefinition
{
    public required string Type { get; init; }

    /// <summary>Параметры генератора (для sequence: prefix/paddingWidth/mode/categoryPrefixes)</summary>
    public JsonObject? Params { get; init; }

    public static MetaFieldGeneratorDefinition? FromOptions(JsonNode? options)
    {
        if (options is not JsonObject obj || obj["generator"] is not JsonObject item)
            return null;

        if (item["type"] is not JsonValue value || !value.TryGetValue<string>(out var type) || string.IsNullOrEmpty(type))
            return null;

        return new MetaFieldGeneratorDefinition
        {
            Type = type,
            Params = item["params"] as JsonObject,
        };
    }
}
