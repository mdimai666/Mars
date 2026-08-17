using Mars.PxBlocks.Host.Shared;
using Mars.PxBlocks.Runtime.Ast;

namespace StandPxBlocksApp.Blocks.Browser;

/// <summary>
/// Контекст «Браузерные скрипты»: сценарии Playwright в системном Edge.
/// Только событие Start (без Loop); политика и блоки уходят редактору через
/// api/PxBlocks/Contexts/browser, состояние запуска (браузер) создаёт
/// PxRunController по имени контекста.
/// </summary>
public static class PxBrowserContext
{
    public const string Name = "browser";

    public static PxEditorContext Create() => PxEditorContext.Define(Name)
        .Title("Браузерные скрипты")
        .Description("Сценарии Playwright в системном Edge: навигация, клики, ввод, ожидания, JavaScript. Всё начинается с блока \"on start\".")
        .Events(PxEvents.Start)
        .EventBlocks(PxEvents.Start)
        .Set<PxBrowserBlocks>()
        .Toolbox(PxBrowserToolbox.Create());
}
