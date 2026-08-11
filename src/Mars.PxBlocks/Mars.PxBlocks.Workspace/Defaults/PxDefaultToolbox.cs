using Mars.PxBlocks.Shared.Toolbox;

namespace Mars.PxBlocks.Workspace.Defaults;

/// <summary>MakeCode-подобный toolbox по умолчанию: стандартные блоки Blockly + демо-категория типов.</summary>
public static class PxDefaultToolbox
{
    public static PxToolbox Create() => new()
    {
        Contents =
        [
            new PxToolboxCategory
            {
                Name = "Тест типов", Colour = "#607D8B",
                Blocks =
                [
                    B("px_create_object"),
                    B("px_demo_number"),
                    B("px_demo_string"),
                    B("px_demo_any"),
                    B("px_demo_object"),
                    B("px_demo_take_number"),
                    B("px_demo_take_any"),
                    B("px_demo_take_object")
                ]
            },
            new PxToolboxCategory
            {
                Name = "Логика", Colour = "#006C9E",
                Blocks =
                [
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
                Name = "Циклы", Colour = "#107C10",
                Blocks =
                [
                    B("controls_repeat_ext"),
                    B("controls_whileUntil"),
                    B("controls_for"),
                    B("controls_forEach"),
                    B("controls_flow_statements")
                ]
            },
            new PxToolboxCategory
            {
                Name = "Математика", Colour = "#712672",
                Blocks =
                [
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
                Name = "Текст", Colour = "#996600",
                Blocks =
                [
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
                Name = "Массивы", Colour = "#5C2D91",
                Blocks =
                [
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
            new PxToolboxCategory { Name = "Переменные", Colour = "#A80000", Custom = "VARIABLE" },
            new PxToolboxCategory { Name = "Функции", Colour = "#7B2FBE", Custom = "PROCEDURE" }
        ]
    };

    private static PxToolboxBlock B(string type) => new() { Type = type };
}
