using System.Text.Json.Nodes;

namespace Mars.PxBlocks.Shared.Toolbox;

public class PxToolboxCategory : PxToolboxItem
{
    public string Name { get; set; } = "";
    public string Colour { get; set; } = "#A8A8A8";
    public string Icon { get; set; } = "";
    public bool Expanded { get; set; }
    public List<PxToolboxBlock> Blocks { get; set; } = [];

    /// <summary>Динамическая категория Blockly: VARIABLE, VARIABLE_DYNAMIC, PROCEDURE.</summary>
    public string? Custom { get; set; }

    internal override JsonNode ToJsonNode()
    {
        var node = new JsonObject
        {
            ["kind"] = "category",
            ["name"] = Name,
            ["expanded"] = Expanded,
            ["contents"] = new JsonArray(Blocks.Select(b => b.ToJsonNode()).Cast<JsonNode?>().ToArray()),
        };
        if (!string.IsNullOrEmpty(Colour))
            node["colour"] = Colour;
        if (!string.IsNullOrEmpty(Custom))
            node["custom"] = Custom;
        return node;
    }
}
