using System.Text.Json;
using System.Text.Json.Nodes;
using Mars.Contracts.PostTypes;

namespace Mars.Cms.Abstractions.Dto.PostTypes;

/// <summary>Сериализация настроек грида в хранимый json (camelCase) и обратно</summary>
public static class PostTypeGridSettingsJson
{
    static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Разбирает хранимый json настроек грида; отсутствует/битый json — null</summary>
    public static PostTypeGridSettings? Parse(JsonNode? node)
    {
        if (node is null) return null;
        try
        {
            return node.Deserialize<PostTypeGridSettings>(Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static JsonNode? ToJsonNode(this PostTypeGridSettings? grid)
        => grid is null ? null : JsonSerializer.SerializeToNode(grid, Options);
}
