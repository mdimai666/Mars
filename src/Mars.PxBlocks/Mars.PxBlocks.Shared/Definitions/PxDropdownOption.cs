using System.Text.Json.Nodes;

namespace Mars.PxBlocks.Shared.Definitions;

/// <summary>Пара Blockly-dropdown: [отображаемый текст, значение].</summary>
public class PxDropdownOption
{
    public string Text { get; set; } = "";
    public string Value { get; set; } = "";

    internal JsonNode ToJsonNode() => new JsonArray(Text, Value);
}
