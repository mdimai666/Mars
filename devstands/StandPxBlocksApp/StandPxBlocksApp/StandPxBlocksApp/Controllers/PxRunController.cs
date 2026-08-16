using System.Net.Mime;
using Mars.PxBlocks.Host.Shared.Dto;
using Mars.PxBlocks.Host.Shared.Services;
using Microsoft.AspNetCore.Mvc;

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
    /// <summary>Старт исполнения: разбор синхронно (ошибка — в Started=false), исполнение фоном, события в хабе.</summary>
    [HttpPost(nameof(Run))]
    public PxRunResponse Run(PxRunRequest request) => runManager.Start(request);

    /// <summary>Остановка исполнения по RunId. false — запуск не найден или уже завершён.</summary>
    [HttpPost(nameof(Stop) + "/{runId:guid}")]
    public bool Stop(Guid runId) => runManager.Stop(runId);
}
