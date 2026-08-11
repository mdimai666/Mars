using System.Text.Json.Nodes;

namespace Mars.PxBlocks.Shared.Definitions;

/// <summary>C-вход для цепочки операторов (input_statement). Пустой Check — принимает любые операторы.</summary>
public class PxStatementInput : PxArg
{
    public string Name { get; set; } = "";
    public List<string> Check { get; set; } = [];

    internal override JsonNode ToJsonNode()
    {
        var node = new JsonObject
        {
            ["type"] = "input_statement",
            ["name"] = Name,
        };
        if (Check.Count > 0)
            node["check"] = new JsonArray(Check.Select(c => (JsonNode?)c).ToArray());
        return node;
    }
}
