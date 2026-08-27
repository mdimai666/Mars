using System.Text.Json.Nodes;

namespace Mars.Cms.Abstractions.Dto.MetaFields;

/// <summary>
/// Определение вычислимого поля <see cref="Mars.Contracts.MetaFields.MetaFieldType.Query"/>,
/// хранится в <see cref="MetaFieldDto.Options"/>
/// </summary>
public record MetaFieldQueryDefinition
{
    /// <summary>
    /// Целевой тип в формате ModelName, например "Post.comment"
    /// </summary>
    public required string Target { get; init; }

    /// <summary>
    /// Key Relation-поля целевого типа, ссылающегося на данный тип (обратная связь)
    /// </summary>
    public required string BackReferenceKey { get; init; }

    /// <summary>
    /// Резерв под QueryLang-фильтр
    /// </summary>
    public string? Filter { get; init; }

    public JsonObject ToOptions()
    {
        var obj = new JsonObject
        {
            ["target"] = Target,
            ["backReferenceKey"] = BackReferenceKey,
        };
        if (!string.IsNullOrEmpty(Filter))
            obj["filter"] = Filter;

        return obj;
    }

    public static MetaFieldQueryDefinition? FromOptions(JsonNode? options)
    {
        if (options is not JsonObject obj) return null;

        var target = ReadString(obj, "target");
        var backReferenceKey = ReadString(obj, "backReferenceKey");
        if (string.IsNullOrEmpty(target) || string.IsNullOrEmpty(backReferenceKey)) return null;

        return new MetaFieldQueryDefinition
        {
            Target = target,
            BackReferenceKey = backReferenceKey,
            Filter = ReadString(obj, "filter"),
        };
    }

    static string? ReadString(JsonObject obj, string name)
        => obj[name] is JsonValue value && value.TryGetValue<string>(out var result) ? result : null;
}
