using System.Text.Json.Nodes;
using Mars.PxBlocks.Runtime.Ast;
using Mars.PxBlocks.Runtime.Values;

namespace Mars.PxBlocks.Runtime.Execution;

/// <summary>
/// Реализация исполнения блока-«листа» по TypeId (аналог INodeImplement&lt;TNode&gt;
/// в Mars.Nodes). Регистрируется в PxBlockImplementsLocator, обычно — целой сборкой.
/// </summary>
public interface IPxBlockImplement
{
    /// <summary>TypeId блока из определения (PxBlockDefinition.TypeId).</summary>
    string TypeId { get; }
}

/// <summary>Реализация блока-выражения (output): вычисляется в PxValue.</summary>
public interface IPxExpressionImplement : IPxBlockImplement
{
    ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call);
}

/// <summary>Реализация блока-оператора (statement): исполняется.</summary>
public interface IPxStatementImplement : IPxBlockImplement
{
    Task ExecuteAsync(PxContext context, PxCall call);
}

/// <summary>Вызов листа: вычисленные входы + поля (dropdown, литерал, переменная) как есть.</summary>
public sealed record PxCall(
    string BlockId,
    IReadOnlyDictionary<string, PxValue> Inputs,
    IReadOnlyDictionary<string, PxFieldData> Fields)
{
    /// <summary>Порядок входов — порядок вычисления (важно для text_join и подобных).</summary>
    public IReadOnlyList<string> InputOrder { get; init; } = [];

    /// <summary>extraState блока (мутаторы) — для листьев с динамической структурой.</summary>
    public JsonNode? ExtraState { get; init; }

    /// <summary>Вход по имени; пустой сокет — Number 0 (дефолт в духе Blockly).</summary>
    public PxValue Input(string name) => Inputs.GetValueOrDefault(name) ?? PxNumberValue.Zero;

    public string FieldText(string name, string fallback = "")
        => Fields.TryGetValue(name, out var field) ? field.Text ?? fallback : fallback;

    public double FieldNumber(string name, double fallback = 0)
        => Fields.TryGetValue(name, out var field) ? field.Number ?? fallback : fallback;

    public string? FieldVariable(string name)
        => Fields.TryGetValue(name, out var field) ? field.VariableId : null;
}
