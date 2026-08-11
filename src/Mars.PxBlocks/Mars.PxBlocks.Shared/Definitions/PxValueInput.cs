using System.Text.Json.Nodes;

namespace Mars.PxBlocks.Shared.Definitions;

/// <summary>Вход для value-блоков (input_value). Пустой Check — принимает любой тип.</summary>
public class PxValueInput : PxArg
{
    public string Name { get; set; } = "";
    public List<string> Check { get; set; } = [];

    internal override JsonNode ToJsonNode()
    {
        var node = new JsonObject
        {
            ["type"] = "input_value",
            ["name"] = Name,
        };
        if (Check.Count > 0)
            node["check"] = new JsonArray(Check.Select(c => (JsonNode?)c).ToArray());
        return node;
    }
}
