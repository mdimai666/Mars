using System.Net.Mime;
using Mars.PxBlocks.Host.Shared.Dto;
using Mars.PxBlocks.Host.Shared.Services;
using Mars.PxBlocks.Shared.Definitions;
using Microsoft.AspNetCore.Mvc;

namespace Mars.PxBlocks.Host.Controllers;

/// <summary>
/// Сервер PxBlocks: определения блоков для редактора, запуск и остановка программ.
/// Организован по образцу NodeController из Mars.Nodes.Host.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class PxBlocksController(IPxRunManager runManager, IPxBlockCatalog catalog) : ControllerBase
{
    /// <summary>Определения блоков и toolbox сервера — редактор получает их при открытии.</summary>
    [HttpGet(nameof(Definitions))]
    public PxDefinitionsResponse Definitions() => new()
    {
        DefinitionsJson = PxBlockDefinition.ToArrayJson(catalog.Definitions),
        Toolbox = catalog.Toolbox
    };

    /// <summary>Запуск программы: разбор синхронно (ошибка — Started=false), исполнение на сервере.</summary>
    [HttpPost(nameof(Run))]
    public PxRunResponse Run(PxRunRequest request) => runManager.Start(request);

    /// <summary>Остановка запуска. false — запуск не найден или уже завершён.</summary>
    [HttpPost(nameof(Stop) + "/{runId:guid}")]
    public bool Stop(Guid runId) => runManager.Stop(runId);
}
