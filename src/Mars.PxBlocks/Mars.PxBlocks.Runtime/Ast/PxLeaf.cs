using System.Text.Json.Nodes;

namespace Mars.PxBlocks.Runtime.Ast;

/// <summary>
/// Блок-«лист»: исполнение — у зарегистрированной в локаторе имплементации
/// (IPxExpressionImplement / IPxStatementImplement). Входы приходят в имплементацию
/// уже вычисленными; поля — как есть.
/// </summary>
public sealed record PxLeafExpression : PxExpression
{
    /// <summary>Входы (сокет → выражение); порядок списка — порядок вычисления.</summary>
    public List<(string Name, PxExpression Expr)> Inputs { get; init; } = [];

    public Dictionary<string, PxFieldData> Fields { get; init; } = [];

    /// <summary>extraState Blockly (мутаторы) — для листьев с динамической структурой.</summary>
    public JsonNode? ExtraState { get; init; }
}

/// <summary>Блок-«лист» в позиции оператора (например text_print).</summary>
public sealed record PxLeafStatement : PxStatement
{
    /// <summary>Входы (сокет → выражение); порядок списка — порядок вычисления.</summary>
    public List<(string Name, PxExpression Expr)> Inputs { get; init; } = [];

    public Dictionary<string, PxFieldData> Fields { get; init; } = [];

    /// <summary>extraState Blockly (мутаторы) — для листьев с динамической структурой.</summary>
    public JsonNode? ExtraState { get; init; }
}
