using System.Text.Json.Nodes;

namespace Mars.PxBlocks.Shared.Toolbox;

/// <summary>Ссылка на блок в toolbox по его типу.</summary>
public class PxToolboxBlock : PxToolboxItem
{
    public string Type { get; set; } = "";

    /// <summary>JSON-проброс значений полей блока, напр. {"NUM": 42}.</summary>
    public string? FieldsJson { get; set; }

    /// <summary>JSON-проброс входов блока (shadow-блоки), формат inputs из Blockly JSON.</summary>
    public string? InputsJson { get; set; }

    internal override JsonNode ToJsonNode()
    {
        var node = new JsonObject
        {
            ["kind"] = "block",
            ["type"] = Type,
        };
        if (!string.IsNullOrEmpty(FieldsJson))
            node["fields"] = JsonNode.Parse(FieldsJson);
        if (!string.IsNullOrEmpty(InputsJson))
            node["inputs"] = JsonNode.Parse(InputsJson);
        return node;
    }
}
