using System.Net.Mime;
using Mars.PxBlocks.Host.Shared.Dto;
using Mars.PxBlocks.Host.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using StandPxBlocksApp.Blocks.Browser;

namespace StandPxBlocksApp.Controllers;

/// <summary>
/// Исполнение программ PxBlocks у стенда: PxBlocks — чисто встраиваемый модуль,
/// REST запуска объявляет хост. Маршрут — как ждёт PxServerRunClient
/// (api/PxBlocks/Run и api/PxBlocks/Stop/{runId}).
/// </summary>
[ApiController]
[Route("api/PxBlocks")]
[Produces(MediaTypeNames.Application.Json)]
public class PxRunController(IPxRunManager runManager) : ControllerBase
{
    /// <summary>
    /// Старт исполнения: разбор синхронно (ошибка — в Started=false), исполнение фоном,
    /// события в хабе. Состояние запуска создаёт хост по имени контекста:
    /// «browser» получает Playwright-браузер (во владение менеджера — диспозится
    /// по завершении; при отказе старта PxRunManager диспозит его сразу).
    /// </summary>
    [HttpPost(nameof(Run))]
    public PxRunResponse Run(PxRunRequest request)
    {
        object? state = request.ContextName == PxBrowserContext.Name
            ? new PxBrowserRunState()
            : null;
        return runManager.Start(request, state);
    }

    /// <summary>Остановка исполнения по RunId. false — запуск не найден или уже завершён.</summary>
    [HttpPost(nameof(Stop) + "/{runId:guid}")]
    public bool Stop(Guid runId) => runManager.Stop(runId);

    /// <summary>Пример сценария контекста «browser» — кнопка «Пример» страницы /browser.</summary>
    [HttpGet("Samples/browser")]
    public ContentResult BrowserSample()
        => Content(PxBrowserSample.WikipediaSearchJson, MediaTypeNames.Application.Json);
}
