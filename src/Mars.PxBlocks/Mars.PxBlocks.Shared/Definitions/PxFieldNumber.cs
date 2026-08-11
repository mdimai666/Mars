using System.Text.Json.Nodes;

namespace Mars.PxBlocks.Shared.Definitions;

public class PxFieldNumber : PxArg
{
    public string Name { get; set; } = "";
    public double Value { get; set; }
    public double? Min { get; set; }
    public double? Max { get; set; }

    internal override JsonNode ToJsonNode()
    {
        var node = new JsonObject
        {
            ["type"] = "field_number",
            ["name"] = Name,
            ["value"] = Value,
        };
        if (Min != null)
            node["min"] = Min.Value;
        if (Max != null)
            node["max"] = Max.Value;
        return node;
    }
}
