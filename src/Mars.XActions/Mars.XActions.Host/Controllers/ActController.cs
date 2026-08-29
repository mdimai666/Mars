using System.Net.Mime;
using Mars.Contracts.Common;
using Mars.Contracts.XActions;
using Mars.Core.Constants;
using Mars.Server.Abstractions.ExceptionFilters;
using Mars.Server.Abstractions.Managers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[NotFoundExceptionFilter]
[FluentValidationExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
public class ActController : ControllerBase
{
    private readonly IActionManager _actionManager;

    public ActController(IActionManager actionManager)
    {
        _actionManager = actionManager;
    }

    [HttpPost("Inject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public Task<XActResult> Inject([FromBody] XActionCommandCall call, CancellationToken cancellationToken)
    {
        return _actionManager.Inject(call.Id, call.Args, cancellationToken);
    }

    /// <summary>
    /// Список команд для UI (палитра, формы вызова): без системных.
    /// </summary>
    [HttpGet("list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IReadOnlyDictionary<string, XActionCommand> List()
    {
        return _actionManager.XActions
            .Where(kv => !kv.Value.System)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    /// <summary>
    /// Динамические варианты выбора для аргументов команд — фронт запрашивает
    /// перед отрисовкой формы.
    /// </summary>
    [HttpGet("options/{sourceKey}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyCollection<XActionOption>> Options(string sourceKey, CancellationToken cancellationToken)
    {
        return _actionManager.GetOptionsAsync(sourceKey, cancellationToken);
    }
}
