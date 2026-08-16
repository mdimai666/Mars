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
            .Tooltip("Открыть страницу по URL и дождаться её загрузки")
            .Message("открыть страницу {URL}", PxMaster.Value("URL", "String")));

        Add(PxMaster.Define("demostand.playwright.click").Colour(Colour)
            .Tooltip("Клик по первому элементу, найденному по селектору")
            .Message("кликнуть по селектору {SELECTOR}", PxMaster.Value("SELECTOR", "String")));

        Add(PxMaster.Define("demostand.playwright.type").Colour(Colour)
            .Tooltip("Ввести текст в поле (input/textarea) по селектору")
            .Message("ввести текст {TEXT} в поле {SELECTOR}",
                PxMaster.Value("TEXT", "String"),
                PxMaster.Value("SELECTOR", "String")));

        Add(PxMaster.Define("demostand.playwright.press").Colour(Colour)
            .Tooltip("Нажать клавишу на клавиатуре")
            .Message("нажать клавишу {KEY}", PxMaster.Dropdown("KEY",
                ("Enter", "Enter"),
                ("Tab", "Tab"),
                ("Escape", "Escape"),
                ("Space", "Space"),
                ("Backspace", "Backspace"),
                ("ArrowDown", "ArrowDown"),
                ("ArrowUp", "ArrowUp"))));

        Add(PxMaster.Define("demostand.playwright.wait_selector").Colour(Colour)
            .Tooltip("Дождаться появления элемента по селектору (таймаут 15 секунд)")
            .Message("ждать элемент {SELECTOR}", PxMaster.Value("SELECTOR", "String")));

        Add(PxMaster.Define("demostand.playwright.wait_ms").Colour(Colour)
            .Tooltip("Пауза в миллисекундах")
            .Message("ждать {MS} мс", PxMaster.Number("MS", 500, min: 0)));

        Add(PxMaster.Define("demostand.playwright.get_text").Output("String").Colour(Colour)
            .Tooltip("Текст первого элемента, найденного по селектору")
            .Message("текст элемента по селектору {SELECTOR}", PxMaster.Value("SELECTOR", "String")));

        Add(PxMaster.Define("demostand.playwright.eval_js").Output("Any").Colour(Colour)
            .Tooltip("Выполнить JavaScript на странице и вернуть результат")
            .Message("выполнить JavaScript {CODE}", PxMaster.Text("CODE", "document.title")));

        Add(PxMaster.Define("demostand.playwright.print_texts").Colour(Colour)
            .Tooltip("Вывести в консоль тексты первых N элементов по селектору")
            .Message("вывести первые {COUNT} текстов по селектору {SELECTOR}",
                PxMaster.Number("COUNT", 3, min: 1),
                PxMaster.Value("SELECTOR", "String")));
    }
}
