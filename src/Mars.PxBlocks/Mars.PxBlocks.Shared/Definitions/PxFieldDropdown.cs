using System.Text.Json.Nodes;

namespace Mars.PxBlocks.Shared.Definitions;

public class PxFieldDropdown : PxArg
{
    public override string Name { get; set; } = "";
    public List<PxDropdownOption> Options { get; set; } = [];

    internal override JsonNode ToJsonNode() => new JsonObject
    {
        ["type"] = "field_dropdown",
        ["name"] = Name,
        ["options"] = new JsonArray(Options.Select(o => o.ToJsonNode()).ToArray()),
    };
}
