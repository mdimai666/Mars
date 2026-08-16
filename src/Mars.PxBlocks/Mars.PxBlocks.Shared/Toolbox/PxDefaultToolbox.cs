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
                Name = "Основное", Colour = "#00838F", Icon = "basic",
                Items =
                [
                    H("Основное"),
                    B("core.events.start"),
                    B("core.events.loop")
                ]
            },
            new PxToolboxCategory
            {
                Name = "Логика", Colour = "#006C9E", Icon = "logic",
                Items =
                [
                    H("Логика"),
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
                Name = "Циклы", Colour = "#107C10", Icon = "loops",
                Items =
                [
                    H("Циклы"),
                    B("core.loops.repeat"),
                    B("core.loops.while_until"),
                    B("core.loops.for"),
                    B("core.loops.for_each"),
                    B("core.loops.flow")
                ]
            },
            new PxToolboxCategory
            {
                Name = "Математика", Colour = "#712672", Icon = "math",
                Items =
                [
                    H("Математика"),
                    B("core.math.number"),
                    B("core.math.arithmetic"),
                    B("core.math.single"),
                    B("core.math.trig"),
                    B("core.math.constant"),
                    B("core.math.number_property"),
                    B("core.math.round"),
                    B("core.math.modulo"),
                    B("core.math.random_int"),
                    B("core.math.random_float")
                ]
            },
            new PxToolboxCategory
            {
                Name = "Текст", Colour = "#996600", Icon = "text",
                Items =
                [
                    H("Текст"),
                    B("core.text.text"),
                    B("core.text.join"),
                    B("core.text.append"),
                    B("core.text.length"),
                    B("core.text.is_empty"),
                    B("core.text.index_of"),
                    B("core.text.char_at"),
                    B("core.text.change_case"),
                    B("core.text.trim"),
                    B("core.text.print")
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
