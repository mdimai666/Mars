namespace Mars.PxBlocks.Shared.Toolbox;

/// <summary>
/// MakeCode-подобный toolbox по умолчанию: событийные блоки + стандартные категории
/// языка (core.* — определения отдаёт сервер, PxStandardBlocks). Массивы и процедуры —
/// пока Blockly-имена (фаза 2). Доменные категории добавляются поверх (на сервере —
/// IPxBlockCatalog.RegisterToolboxCategory, в редакторе — свой параметр Toolbox).
/// </summary>
public static class PxDefaultToolbox
{
    public static PxToolbox Create() => new()
    {
        Contents =
        [
            new PxToolboxCategory
            {
                Name = "Basic", Colour = "#00838F", Icon = "basic",
                Items =
                [
                    H("Basic"),
                    B("core.events.start"),
                    B("core.events.loop")
                ]
            },
            new PxToolboxCategory
            {
                Name = "Logic", Colour = "#006C9E", Icon = "logic",
                Items =
                [
                    H("Logic"),
                    B("core.logic.if"),
                    B("core.logic.if_else"),
                    B("core.logic.compare"),
                    B("core.logic.operation"),
                    B("core.logic.negate"),
                    B("core.logic.boolean"),
                    B("core.logic.null"),
                    B("core.logic.ternary")
                ]
            },
            new PxToolboxCategory
            {
                Name = "Loops", Colour = "#107C10", Icon = "loops",
                Items =
                [
                    H("Loops"),
                    B("core.loops.repeat"),
                    B("core.loops.while_until"),
                    B("core.loops.for"),
                    B("core.loops.for_each"),
                    B("core.loops.flow"),
                    B("core.loops.pause")
                ]
            },
            new PxToolboxCategory
            {
                Name = "Math", Colour = "#712672", Icon = "math",
                Items =
                [
                    H("Math"),
                    B("core.math.number"),
                    B("core.math.arithmetic"),
                    B("core.math.single"),
                    B("core.math.trig"),
                    B("core.math.constant"),
                    B("core.math.number_property"),
                    B("core.math.round"),
                    B("core.math.modulo"),
                    B("core.math.random_int"),
                    B("core.math.random_float"),
                    B("core.math.min_max"),
                    new PxToolboxBlock
                    {
                        Type = "core.math.map",
                        InputsJson = """{"VALUE":{"shadow":{"type":"core.math.number","fields":{"NUM":0}}},"FROM_LOW":{"shadow":{"type":"core.math.number","fields":{"NUM":0}}},"FROM_HIGH":{"shadow":{"type":"core.math.number","fields":{"NUM":1023}}},"TO_LOW":{"shadow":{"type":"core.math.number","fields":{"NUM":0}}},"TO_HIGH":{"shadow":{"type":"core.math.number","fields":{"NUM":4}}}}"""
                    }
                ]
            },
            new PxToolboxCategory
            {
                Name = "Text", Colour = "#996600", Icon = "text",
                Items =
                [
                    H("Text"),
                    B("core.text.text"),
                    B("core.text.join"),
                    B("core.text.append"),
                    B("core.text.length"),
                    B("core.text.is_empty"),
                    B("core.text.index_of"),
                    B("core.text.char_at"),
                    B("core.text.change_case"),
                    B("core.text.trim"),
                    new PxToolboxBlock
                    {
                        Type = "core.text.substring",
                        InputsJson = """{"VALUE":{"shadow":{"type":"core.text.text","fields":{"TEXT":""}}},"START":{"shadow":{"type":"core.math.number","fields":{"NUM":0}}},"LENGTH":{"shadow":{"type":"core.math.number","fields":{"NUM":1}}}}"""
                    },
                    new PxToolboxBlock
                    {
                        Type = "core.text.includes",
                        InputsJson = """{"VALUE":{"shadow":{"type":"core.text.text","fields":{"TEXT":""}}},"FIND":{"shadow":{"type":"core.text.text","fields":{"TEXT":""}}}}"""
                    },
                    new PxToolboxBlock
                    {
                        Type = "core.text.compare",
                        InputsJson = """{"A":{"shadow":{"type":"core.text.text","fields":{"TEXT":""}}},"B":{"shadow":{"type":"core.text.text","fields":{"TEXT":""}}}}"""
                    },
                    new PxToolboxBlock
                    {
                        Type = "core.text.split",
                        InputsJson = """{"VALUE":{"shadow":{"type":"core.text.text","fields":{"TEXT":""}}},"SEPARATOR":{"shadow":{"type":"core.text.text","fields":{"TEXT":","}}}}"""
                    },
                    new PxToolboxBlock
                    {
                        Type = "core.text.parse",
                        InputsJson = """{"VALUE":{"shadow":{"type":"core.text.text","fields":{"TEXT":"123"}}}}"""
                    },
                    new PxToolboxBlock
                    {
                        Type = "core.text.char_code",
                        InputsJson = """{"VALUE":{"shadow":{"type":"core.text.text","fields":{"TEXT":""}}},"INDEX":{"shadow":{"type":"core.math.number","fields":{"NUM":0}}}}"""
                    },
                    B("core.text.print")
                ]
            },
            new PxToolboxCategory
            {
                // Набор MakeCode (0-based): create_empty/create_with/repeat/length —
                // встроенные блоки Blockly с Msg-лейблами MakeCode (JsSrc/index.ts),
                // get/set/indexof — серверные определения PxStandardBlocks.
                Name = "Arrays", Colour = "#5C2D91", Icon = "arrays",
                Items =
                [
                    H("Arrays"),
                    B("lists_create_empty"),
                    B("lists_create_with"),
                    B("lists_repeat"),
                    new PxToolboxBlock
                    {
                        Type = "lists_length",
                        InputsJson = """{"VALUE":{"shadow":{"type":"lists_create_empty"}}}"""
                    },
                    new PxToolboxBlock
                    {
                        Type = "array_indexof",
                        InputsJson = """{"LIST":{"shadow":{"type":"lists_create_empty"}},"VALUE":{"shadow":{"type":"core.text.text","fields":{"TEXT":""}}}}"""
                    },
                    new PxToolboxBlock
                    {
                        Type = "lists_index_get",
                        InputsJson = """{"LIST":{"shadow":{"type":"lists_create_empty"}},"INDEX":{"shadow":{"type":"core.math.number","fields":{"NUM":0}}}}"""
                    },
                    new PxToolboxBlock
                    {
                        Type = "lists_index_set",
                        InputsJson = """{"LIST":{"shadow":{"type":"lists_create_empty"}},"INDEX":{"shadow":{"type":"core.math.number","fields":{"NUM":0}}},"VALUE":{"shadow":{"type":"core.text.text","fields":{"TEXT":""}}}}"""
                    },
                    new PxToolboxBlock
                    {
                        Type = "array_push",
                        InputsJson = """{"LIST":{"shadow":{"type":"lists_create_empty"}},"VALUE":{"shadow":{"type":"core.text.text","fields":{"TEXT":""}}}}"""
                    },
                    new PxToolboxBlock { Type = "array_pop", InputsJson = """{"LIST":{"shadow":{"type":"lists_create_empty"}}}""" },
                    new PxToolboxBlock { Type = "array_pop_statement", InputsJson = """{"LIST":{"shadow":{"type":"lists_create_empty"}}}""" },
                    new PxToolboxBlock { Type = "array_shift", InputsJson = """{"LIST":{"shadow":{"type":"lists_create_empty"}}}""" },
                    new PxToolboxBlock { Type = "array_shift_statement", InputsJson = """{"LIST":{"shadow":{"type":"lists_create_empty"}}}""" },
                    new PxToolboxBlock
                    {
                        Type = "array_unshift",
                        InputsJson = """{"LIST":{"shadow":{"type":"lists_create_empty"}},"VALUE":{"shadow":{"type":"core.text.text","fields":{"TEXT":""}}}}"""
                    },
                    new PxToolboxBlock
                    {
                        Type = "array_unshift_statement",
                        InputsJson = """{"LIST":{"shadow":{"type":"lists_create_empty"}},"VALUE":{"shadow":{"type":"core.text.text","fields":{"TEXT":""}}}}"""
                    },
                    new PxToolboxBlock
                    {
                        Type = "array_insertAt",
                        InputsJson = """{"LIST":{"shadow":{"type":"lists_create_empty"}},"INDEX":{"shadow":{"type":"core.math.number","fields":{"NUM":0}}},"VALUE":{"shadow":{"type":"core.text.text","fields":{"TEXT":""}}}}"""
                    },
                    new PxToolboxBlock
                    {
                        Type = "array_removeat",
                        InputsJson = """{"LIST":{"shadow":{"type":"lists_create_empty"}},"INDEX":{"shadow":{"type":"core.math.number","fields":{"NUM":0}}}}"""
                    },
                    new PxToolboxBlock
                    {
                        Type = "array_removeat_statement",
                        InputsJson = """{"LIST":{"shadow":{"type":"lists_create_empty"}},"INDEX":{"shadow":{"type":"core.math.number","fields":{"NUM":0}}}}"""
                    },
                    new PxToolboxBlock { Type = "array_pickRandom", InputsJson = """{"LIST":{"shadow":{"type":"lists_create_empty"}}}""" },
                    new PxToolboxBlock { Type = "array_reverse", InputsJson = """{"LIST":{"shadow":{"type":"lists_create_empty"}}}""" }
                ]
            },
            new PxToolboxSeparator(),
            new PxToolboxCategory { Name = "Variables", Colour = "#A80000", Icon = "variables", Custom = "VARIABLE" },
            new PxToolboxCategory { Name = "Functions", Colour = "#7B2FBE", Icon = "functions", Custom = "PROCEDURE" }
        ]
    };

    private static PxToolboxBlock B(string type) => new() { Type = type };

    /// <summary>Заголовок раздела во flyout, как в MakeCode.</summary>
    private static PxToolboxLabel H(string text) => new() { Text = text, WebClass = "blocklyFlyoutHeading" };
}
