namespace Mars.PxBlocks.Shared.Toolbox;

/// <summary>
/// MakeCode-подобный toolbox по умолчанию: событийные блоки + стандартные категории
/// Blockly. Доменные категории добавляются поверх (на сервере — IPxBlockCatalog.RegisterToolboxCategory,
/// в редакторе — свой параметр Toolbox).
/// </summary>
public static class PxDefaultToolbox
{
    public static PxToolbox Create() => new()
    {
        Contents =
        [
            new PxToolboxCategory
            {
                Name = "Основное", Colour = "#00838F", Icon = "basic",
                Items =
                [
                    H("Основное"),
                    B("px_start"),
                    B("px_loop")
                ]
            },
            new PxToolboxCategory
            {
                Name = "Логика", Colour = "#006C9E", Icon = "logic",
                Items =
                [
                    H("Логика"),
                    B("controls_if"),
                    B("controls_if_else"),
                    B("logic_compare"),
                    B("logic_operation"),
                    B("logic_negate"),
                    B("logic_boolean"),
                    B("logic_null"),
                    B("logic_ternary")
                ]
            },
            new PxToolboxCategory
            {
                Name = "Циклы", Colour = "#107C10", Icon = "loops",
                Items =
                [
                    H("Циклы"),
                    B("controls_repeat_ext"),
                    B("controls_whileUntil"),
                    B("controls_for"),
                    B("controls_forEach"),
                    B("controls_flow_statements")
                ]
            },
            new PxToolboxCategory
            {
                Name = "Математика", Colour = "#712672", Icon = "math",
                Items =
                [
                    H("Математика"),
                    B("math_number"),
                    B("math_arithmetic"),
                    B("math_single"),
                    B("math_trig"),
                    B("math_constant"),
                    B("math_number_property"),
                    B("math_round"),
                    B("math_modulo"),
                    B("math_random_int"),
                    B("math_random_float")
                ]
            },
            new PxToolboxCategory
            {
                Name = "Текст", Colour = "#996600", Icon = "text",
                Items =
                [
                    H("Текст"),
                    B("text"),
                    B("text_join"),
                    B("text_append"),
                    B("text_length"),
                    B("text_isEmpty"),
                    B("text_indexOf"),
                    B("text_charAt"),
                    B("text_changeCase"),
                    B("text_trim"),
                    B("text_print")
                ]
            },
            new PxToolboxCategory
            {
                Name = "Массивы", Colour = "#5C2D91", Icon = "arrays",
                Items =
                [
                    H("Массивы"),
                    B("lists_create_with"),
                    B("lists_repeat"),
                    B("lists_length"),
                    B("lists_isEmpty"),
                    B("lists_indexOf"),
                    B("lists_getIndex"),
                    B("lists_setIndex"),
                    B("lists_getSublist"),
                    B("lists_split"),
                    B("lists_sort")
                ]
            },
            new PxToolboxSeparator(),
            new PxToolboxCategory { Name = "Переменные", Colour = "#A80000", Icon = "variables", Custom = "VARIABLE" },
            new PxToolboxCategory { Name = "Функции", Colour = "#7B2FBE", Icon = "functions", Custom = "PROCEDURE" }
        ]
    };

    private static PxToolboxBlock B(string type) => new() { Type = type };

    /// <summary>Заголовок раздела во flyout, как в MakeCode.</summary>
    private static PxToolboxLabel H(string text) => new() { Text = text, WebClass = "blocklyFlyoutHeading" };
}
