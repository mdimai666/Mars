using System.Text.Json.Nodes;

namespace Mars.PxBlocks.Shared.Definitions;

/// <summary>
/// Определение блока: источник правды, из которого генерируется Blockly JSON definition.
/// Новые блоки создаются наследованием; исполнение блоков будет жить отдельно (по образцу INodeImplement).
/// </summary>
public class PxBlockDefinition
{
    public string TypeId { get; set; } = "";
    public string Colour { get; set; } = "#A8A8A8";
    public string Tooltip { get; set; } = "";

    /// <summary>Строки message0/args0, message1/args1, … в терминах Blockly.</summary>
    public List<PxMessageRow> Messages { get; set; } = [];

    /// <summary>Тип выходного коннектора. null — блок-оператор (statement).</summary>
    public string? OutputType { get; set; }

    public bool HasPrevious { get; set; } = true;
    public bool HasNext { get; set; } = true;

    /// <summary>Имена расширений Blockly (Blockly.Extensions.register).</summary>
    public List<string> Extensions { get; set; } = [];

    /// <summary>Имя мутатора Blockly (Blockly.Extensions.registerMutator) — для блоков с динамической структурой.</summary>
    public string? Mutator { get; set; }

    public virtual string ToJson()
    {
        var node = new JsonObject { ["type"] = TypeId };

        for (var i = 0; i < Messages.Count; i++)
        {
            node[$"message{i}"] = Messages[i].Message;
            if (Messages[i].Args.Count > 0)
                node[$"args{i}"] = new JsonArray(Messages[i].Args.Select(a => a.ToJsonNode()).ToArray());
        }

        if (!string.IsNullOrEmpty(Tooltip))
            node["tooltip"] = Tooltip;

        node["colour"] = Colour;

        if (OutputType != null)
        {
            node["output"] = OutputType;
        }
        else
        {
            if (HasPrevious)
                node["previousStatement"] = null;
            if (HasNext)
                node["nextStatement"] = null;
        }

        if (Extensions.Count > 0)
            node["extensions"] = new JsonArray(Extensions.Select(e => (JsonNode?)e).ToArray());

        if (Mutator != null)
            node["mutator"] = Mutator;

        return node.ToJsonString();
    }

    public static string ToArrayJson(IEnumerable<PxBlockDefinition> definitions) =>
        new JsonArray(definitions.Select(d => JsonNode.Parse(d.ToJson())).ToArray()).ToJsonString();
}
