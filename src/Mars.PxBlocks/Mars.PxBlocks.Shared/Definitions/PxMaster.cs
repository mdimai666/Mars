namespace Mars.PxBlocks.Shared.Definitions;

/// <summary>
/// Точка входа fluent-API определений блоков: блок объявляется одним выражением
/// <c>PxMaster.Define("id").Message("текст {arg}", PxMaster.Number("arg"))</c> — аналог
/// аннотаций на функциях в PXT. Фабрики <see cref="Number"/>, <see cref="Text"/> и др.
/// создают аргументы.
/// </summary>
public static class PxMaster
{
    public static PxBlockBuilder Define(string typeId) => new(typeId);

    /// <summary>Числовое поле (field_number). defl — значение по умолчанию.</summary>
    public static PxFieldNumber Number(string name, double defl = 0, double? min = null, double? max = null) =>
        new() { Name = name, Value = defl, Min = min, Max = max };

    /// <summary>Текстовое поле (field_input). defl — значение по умолчанию.</summary>
    public static PxFieldText Text(string name, string defl = "") =>
        new() { Name = name, Text = defl };

    /// <summary>Выпадающий список (field_dropdown): пары (отображаемый текст, значение).</summary>
    public static PxFieldDropdown Dropdown(string name, params (string Text, string Value)[] options) =>
        new() { Name = name, Options = options.Select(o => new PxDropdownOption { Text = o.Text, Value = o.Value }).ToList() };

    /// <summary>Вход для value-блоков (input_value). Пустой check — принимает любой тип.</summary>
    public static PxValueInput Value(string name, params string[] check) =>
        new() { Name = name, Check = [.. check] };

    /// <summary>C-вход для цепочки операторов (input_statement). Пустой check — любые операторы.</summary>
    public static PxStatementInput Do(string name, params string[] check) =>
        new() { Name = name, Check = [.. check] };
}
