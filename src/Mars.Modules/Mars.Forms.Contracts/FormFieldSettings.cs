using System.Text.Json;
using System.Text.Json.Nodes;

namespace Mars.Forms.Contracts;

/// <summary>
/// Параметры поля, у которого нет собственной записи определения (системные слоты поста,
/// колонки внешних таблиц): правила и редактор хранятся у владельца формы, а не в раскладке.
/// Раскладка (<see cref="FormLayoutSettings"/>) отвечает только за представление —
/// порядок, зону, видимость, ширину, секции.
/// </summary>
public record FormFieldSettings
{
    /// <summary>Ключ поля (для поста — ключ слота <c>SystemFieldsCatalog</c>)</summary>
    public required string Key { get; init; }

    /// <summary>Переопределение редактора; null — редактор по умолчанию для типа</summary>
    public string? Editor { get; init; }

    public IReadOnlyCollection<FormRuleDefinition> Rules { get; init; } = [];
}

/// <summary>
/// Сериализация набора параметров полей в json (camelCase) — по образцу <see cref="FormLayoutJson"/>.
/// Пустой набор не хранится: ключ из опций владельца убирается.
/// </summary>
public static class FormFieldSettingsJson
{
    static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Разбирает хранимый json; отсутствует или битый json — null</summary>
    public static IReadOnlyCollection<FormFieldSettings>? Parse(JsonNode? node)
    {
        if (node is null) return null;
        try
        {
            return node.Deserialize<List<FormFieldSettings>>(Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static JsonNode? ToJsonNode(this IReadOnlyCollection<FormFieldSettings>? settings)
        => settings is null || settings.Count == 0
            ? null
            : JsonSerializer.SerializeToNode(settings, Options);
}
