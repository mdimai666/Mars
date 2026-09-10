using System.Text.Json.Nodes;

namespace Mars.PxBlocks.Core.Toolbox;

public class PxToolboxSeparator : PxToolboxItem
{
    public string? Colour { get; set; }

    internal override JsonNode ToJsonNode()
    {
        var node = new JsonObject { ["kind"] = "sep" };
        if (!string.IsNullOrEmpty(Colour))
            node["colour"] = Colour;
        return node;
    }
}
