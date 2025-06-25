using System.ComponentModel;
using System.Net.Mime;
using Mars.Core.Constants;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Host.Shared.Managers;
using Mars.Shared.Common;
using Mars.Shared.Contracts.XActions;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Controllers;

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

    [HttpPost("Inject/{actionId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public Task<XActResult> Inject(string actionId, [FromBody][DefaultValue("[]")] string[] args, CancellationToken cancellationToken)
    {
        return _actionManager.Inject(actionId, args, cancellationToken);
    }
}
