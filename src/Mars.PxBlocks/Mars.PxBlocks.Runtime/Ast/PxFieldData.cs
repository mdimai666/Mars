namespace Mars.PxBlocks.Runtime.Ast;

/// <summary>
/// Поле блока Blockly: текст/число (field_input, field_number, dropdown)
/// либо ссылка на переменную (field_variable).
/// </summary>
public sealed record PxFieldData
{
    public string? Text { get; init; }

    public double? Number { get; init; }

    /// <summary>Id переменной (блоки variables_get/set, controls_for, text_append, …).</summary>
    public string? VariableId { get; init; }

    public static PxFieldData OfText(string text) => new() { Text = text };

    public static PxFieldData OfNumber(double number) => new() { Number = number };

    public static PxFieldData OfVariable(string variableId) => new() { VariableId = variableId };
}
