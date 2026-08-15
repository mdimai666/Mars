using System.Net.Mime;
using Mars.PxBlocks.Host.Shared.Dto;
using Mars.PxBlocks.Host.Shared.Services;
using Mars.PxBlocks.Shared.Definitions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Mars.PxBlocks.Host.Controllers;

/// <summary>
/// Сервер PxBlocks: определения блоков для редактора, запуск и остановка программ.
/// Организован по образцу NodeController из Mars.Nodes.Host.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class PxBlocksController(IPxRunManager runManager, IPxBlockCatalog catalog, IPxEditorContextRegistry contexts) : ControllerBase
{
    /// <summary>Определения блоков и toolbox сервера — редактор получает их при открытии.</summary>
    [HttpGet(nameof(Definitions))]
    public PxDefinitionsResponse Definitions() => new()
    {
        DefinitionsJson = PxBlockDefinition.ToArrayJson(catalog.Definitions),
        Toolbox = catalog.Toolbox
    };

    /// <summary>Зарегистрированные контексты редакторов (определения — в Contexts/{name}).</summary>
    [HttpGet(nameof(Contexts))]
    public IReadOnlyList<PxEditorContextInfo> Contexts() =>
        contexts.Contexts
            .Select(context => new PxEditorContextInfo
            {
                Name = context.Name,
                Title = context.Title,
                Description = context.Description
            })
            .ToList();

    /// <summary>Определения и toolbox контекста — их получает встраиваемый редактор.</summary>
    [HttpGet(nameof(Contexts) + "/{name}")]
    [ProducesResponseType<PxDefinitionsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<PxDefinitionsResponse> ContextDefinitions(string name)
    {
        var context = contexts.Get(name);
        if (context == null)
            return NotFound();

        return new PxDefinitionsResponse
        {
            DefinitionsJson = PxBlockDefinition.ToArrayJson(context.EffectiveDefinitions),
            Toolbox = context.EffectiveToolbox
        };
    }

    /// <summary>Запуск программы: разбор синхронно (ошибка — Started=false), исполнение на сервере.</summary>
    [HttpPost(nameof(Run))]
    public PxRunResponse Run(PxRunRequest request) => runManager.Start(request);

    /// <summary>Остановка запуска. false — запуск не найден или уже завершён.</summary>
    [HttpPost(nameof(Stop) + "/{runId:guid}")]
    public bool Stop(Guid runId) => runManager.Stop(runId);
}
