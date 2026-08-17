using System.Text.Json.Nodes;

namespace Mars.PxBlocks.Shared.Definitions;

/// <summary>
/// Поле-переменная (field_variable): выбор переменной из dropdown-а workspace.
/// В сериализации Blockly хранит id переменной — парсер читает
/// <c>fields.ИМЯ.id</c> (PxParser.FieldVariableId).
/// </summary>
public class PxFieldVariable : PxArg
{
    public override string Name { get; set; } = "";

    /// <summary>Имя переменной по умолчанию (создаётся при первом использовании блока).</summary>
    public string Variable { get; set; } = "item";

    internal override JsonNode ToJsonNode() => new JsonObject
    {
        ["type"] = "field_variable",
        ["name"] = Name,
        ["variable"] = Variable,
    };
}
