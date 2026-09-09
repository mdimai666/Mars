using System.Text.Json;
using System.Text.Json.Nodes;

namespace Mars.Forms.Contracts;

/// <summary>
/// Сериализация хранимой раскладки формы в json (camelCase) и обратно —
/// по образцу <c>PostTypeGridSettingsJson</c>. Хранится только раскладка
/// (порядок, зоны, видимость, правила полей с <see cref="FormFieldDescriptor.SettingsOnForm"/>),
/// дескрипторы не хранятся.
/// </summary>
public static class FormLayoutJson
{
    static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Разбирает хранимый json раскладки; отсутствует/битый json — null</summary>
    public static FormLayoutSettings? Parse(JsonNode? node)
    {
        if (node is null) return null;
        try
        {
            return node.Deserialize<FormLayoutSettings>(Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static JsonNode? ToJsonNode(this FormLayoutSettings? settings)
        => settings is null ? null : JsonSerializer.SerializeToNode(settings, Options);
}
