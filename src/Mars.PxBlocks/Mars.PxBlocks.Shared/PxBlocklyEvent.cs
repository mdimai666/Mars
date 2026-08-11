using System.Text.Json.Serialization;

namespace Mars.PxBlocks.Shared;

/// <summary>Событие Blockly в пакетированном виде из JS-моста.</summary>
public class PxBlocklyEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("blockId")]
    public string? BlockId { get; set; }

    [JsonPropertyName("ids")]
    public List<string>? Ids { get; set; }
}
