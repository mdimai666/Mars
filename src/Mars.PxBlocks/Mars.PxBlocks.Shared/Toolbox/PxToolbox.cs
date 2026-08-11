using System.Text.Json.Nodes;

namespace Mars.PxBlocks.Shared.Toolbox;

/// <summary>
/// Модель toolbox. Сериализуется в JSON-формат Blockly (kind: categoryToolbox/flyoutToolbox).
/// </summary>
public class PxToolbox
{
    public List<PxToolboxItem> Contents { get; set; } = [];

    public string ToJson()
    {
        var root = new JsonObject
        {
            ["kind"] = Contents.Any(i => i is PxToolboxCategory) ? "categoryToolbox" : "flyoutToolbox",
            ["contents"] = new JsonArray(Contents.Select(i => i.ToJsonNode()).Cast<JsonNode?>().ToArray()),
        };
        return root.ToJsonString();
    }
}
