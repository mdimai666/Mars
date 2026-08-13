using System.Text.Json.Nodes;

namespace Mars.PxBlocks.Shared.Toolbox;

public class PxToolboxCategory : PxToolboxItem
{
    public string Name { get; set; } = "";
    public string Colour { get; set; } = "#A8A8A8";

    /// <summary>Имя иконки для рейки редактора (в JSON Blockly не попадает).</summary>
    public string Icon { get; set; } = "";

    /// <summary>Категория раздела «Advanced» рейки (в JSON Blockly не попадает).</summary>
    public bool Advanced { get; set; }

    public bool Expanded { get; set; }

    /// <summary>Содержимое flyout: блоки, метки-заголовки, разделители.</summary>
    public List<PxToolboxItem> Items { get; set; } = [];

    /// <summary>Динамическая категория Blockly: VARIABLE, VARIABLE_DYNAMIC, PROCEDURE.</summary>
    public string? Custom { get; set; }

    internal override JsonNode ToJsonNode()
    {
        var node = new JsonObject
        {
            ["kind"] = "category",
            ["name"] = Name,
            ["expanded"] = Expanded,
            ["contents"] = new JsonArray(Items.Select(i => i.ToJsonNode()).Cast<JsonNode?>().ToArray()),
        };
        if (!string.IsNullOrEmpty(Colour))
            node["colour"] = Colour;
        if (!string.IsNullOrEmpty(Custom))
            node["custom"] = Custom;
        return node;
    }
}
