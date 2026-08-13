using System.Text.Json.Nodes;

namespace Mars.PxBlocks.Shared.Definitions;

public class PxFieldText : PxArg
{
    public override string Name { get; set; } = "";
    public string Text { get; set; } = "";

    internal override JsonNode ToJsonNode() => new JsonObject
    {
        ["type"] = "field_input",
        ["name"] = Name,
        ["text"] = Text,
    };
}
