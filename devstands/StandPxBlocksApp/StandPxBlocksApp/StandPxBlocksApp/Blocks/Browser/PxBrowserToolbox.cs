using Mars.PxBlocks.Shared.Toolbox;

namespace StandPxBlocksApp.Blocks.Browser;

/// <summary>
/// Свой toolbox контекста «browser»: дефолт не подходит — в его «Основном»
/// есть core.events.loop, а в контексте только Start. Строки в сокетах браузерных
/// блоков — shadow-блоки (InputsJson): редактируются прямо в блоке и при
/// желании заменяются переменной или склейкой строк.
/// </summary>
public static class PxBrowserToolbox
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
                    B("core.events.start")
                ]
            },
            new PxToolboxCategory
            {
                Name = "Browser", Colour = "#1976D2", Icon = "globe",
                Items =
                [
                    H("Navigation and actions"),
                    new PxToolboxBlock
                    {
                        Type = "demostand.playwright.goto",
                        InputsJson = """{"URL":{"shadow":{"type":"core.text.text","fields":{"TEXT":"https://ru.wikipedia.org"}}}}"""
                    },
                    new PxToolboxBlock
                    {
                        Type = "demostand.playwright.click",
                        InputsJson = """{"SELECTOR":{"shadow":{"type":"core.text.text","fields":{"TEXT":"button"}}}}"""
                    },
                    new PxToolboxBlock
                    {
                        Type = "demostand.playwright.type",
                        InputsJson = """
                        {
                          "TEXT": {"shadow": {"type": "core.text.text", "fields": {"TEXT": "text"}}},
                          "SELECTOR": {"shadow": {"type": "core.text.text", "fields": {"TEXT": "#input"}}}
                        }
                        """
                    },
                    B("demostand.playwright.press"),
                    new PxToolboxBlock
                    {
                        Type = "demostand.playwright.wait_selector",
                        InputsJson = """{"SELECTOR":{"shadow":{"type":"core.text.text","fields":{"TEXT":".result"}}}}"""
                    },
                    B("demostand.playwright.wait_ms"),
                    H("Data"),
                    new PxToolboxBlock
                    {
                        Type = "demostand.playwright.get_text",
                        InputsJson = """{"SELECTOR":{"shadow":{"type":"core.text.text","fields":{"TEXT":"h1"}}}}"""
                    },
                    B("demostand.playwright.eval_js"),
                    new PxToolboxBlock
                    {
                        Type = "demostand.playwright.print_texts",
                        InputsJson = """{"SELECTOR":{"shadow":{"type":"core.text.text","fields":{"TEXT":".item"}}}}"""
                    }
                ]
            },
            new PxToolboxCategory
            {
                Name = "Logic", Colour = "#006C9E", Icon = "logic",
                Items =
                [
                    H("Logic"),
                    B("core.logic.if"),
                    B("core.logic.compare"),
                    B("core.logic.operation"),
                    B("core.logic.negate"),
                    B("core.logic.boolean")
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
                    B("core.text.length"),
                    B("core.text.print")
                ]
            },
            new PxToolboxSeparator(),
            new PxToolboxCategory { Name = "Variables", Colour = "#A80000", Icon = "variables", Custom = "VARIABLE" },
            new PxToolboxCategory { Name = "Functions", Colour = "#7B2FBE", Icon = "functions", Custom = "PROCEDURE" }
        ]
    };

    private static PxToolboxBlock B(string type) => new() { Type = type };

    private static PxToolboxLabel H(string text) => new() { Text = text, WebClass = "blocklyFlyoutHeading" };
}
