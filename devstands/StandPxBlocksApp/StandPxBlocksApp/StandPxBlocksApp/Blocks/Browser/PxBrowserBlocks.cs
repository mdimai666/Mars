using Mars.PxBlocks.Shared.Definitions;

namespace StandPxBlocksApp.Blocks.Browser;

/// <summary>
/// Браузерные скрипты: блоки Playwright-действий для контекста «browser».
/// Объявляются ТОЛЬКО на сервере, редактор получает их через
/// api/PxBlocks/Contexts/browser. Селекторы/тексты — value-входы String:
/// из тулбокса приходят с shadow-текстом (редактируется прямо в сокете),
/// при желании заменяются переменной или склейкой строк.
/// </summary>
public sealed class PxBrowserBlocks : PxBlockSet
{
    private const string Colour = "#1976D2";

    public PxBrowserBlocks()
    {
        Add(PxMaster.Define("demostand.playwright.goto").Colour(Colour)
            .Tooltip("Open a page by URL and wait for it to load")
            .Message("open page {URL}", PxMaster.Value("URL", "String")));

        Add(PxMaster.Define("demostand.playwright.click").Colour(Colour)
            .Tooltip("Click the first element matching the selector")
            .Message("click {SELECTOR}", PxMaster.Value("SELECTOR", "String")));

        Add(PxMaster.Define("demostand.playwright.type").Colour(Colour)
            .Tooltip("Type text into a field (input/textarea) by selector")
            .Message("type {TEXT} into {SELECTOR}",
                PxMaster.Value("TEXT", "String"),
                PxMaster.Value("SELECTOR", "String")));

        Add(PxMaster.Define("demostand.playwright.press").Colour(Colour)
            .Tooltip("Press a keyboard key")
            .Message("press key {KEY}", PxMaster.Dropdown("KEY",
                ("Enter", "Enter"),
                ("Tab", "Tab"),
                ("Escape", "Escape"),
                ("Space", "Space"),
                ("Backspace", "Backspace"),
                ("ArrowDown", "ArrowDown"),
                ("ArrowUp", "ArrowUp"))));

        Add(PxMaster.Define("demostand.playwright.wait_selector").Colour(Colour)
            .Tooltip("Wait for an element matching the selector to appear (15 second timeout)")
            .Message("wait for {SELECTOR}", PxMaster.Value("SELECTOR", "String")));

        Add(PxMaster.Define("demostand.playwright.wait_ms").Colour(Colour)
            .Tooltip("Pause in milliseconds")
            .Message("wait {MS} ms", PxMaster.Number("MS", 500, min: 0)));

        Add(PxMaster.Define("demostand.playwright.get_text").Output("String").Colour(Colour)
            .Tooltip("Text of the first element matching the selector")
            .Message("text of {SELECTOR}", PxMaster.Value("SELECTOR", "String")));

        Add(PxMaster.Define("demostand.playwright.eval_js").Output("Any").Colour(Colour)
            .Tooltip("Run JavaScript on the page and return the result")
            .Message("evaluate JavaScript {CODE}", PxMaster.Text("CODE", "document.title")));

        Add(PxMaster.Define("demostand.playwright.print_texts").Colour(Colour)
            .Tooltip("Print the texts of the first N elements matching the selector")
            .Message("print first {COUNT} texts of {SELECTOR}",
                PxMaster.Number("COUNT", 3, min: 1),
                PxMaster.Value("SELECTOR", "String")));
    }
}
