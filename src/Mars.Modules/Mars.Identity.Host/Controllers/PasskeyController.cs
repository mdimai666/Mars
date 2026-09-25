using System.Net.Mime;
using Mars.Contracts.Common;
using Mars.Identity.Abstractions.Interfaces;
using Mars.Identity.Abstractions.Mappings.Accounts;
using Mars.Identity.Abstractions.Mappings.Passkeys;
using Mars.Identity.Abstractions.Services;
using Mars.Identity.Contracts.Auth;
using Mars.Identity.Contracts.Options;
using Mars.Identity.Contracts.Passkeys;
using Mars.Server.Abstractions.ExceptionFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Mars.Identity.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[NotFoundExceptionFilter]
[FluentValidationExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
public class PasskeyController : ControllerBase
{
    private readonly IPasskeyService _passkeyService;
    private readonly IRequestContext _requestContext;

    public PasskeyController(IPasskeyService passkeyService, IRequestContext requestContext)
    {
        _passkeyService = passkeyService;
        _requestContext = requestContext;
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IReadOnlyCollection<PasskeySummaryResponse>> List()
    {
        var passkeys = await _passkeyService.List(CurrentUserId);
        return passkeys.Select(s => s.ToResponse()).ToList();
    }

    [HttpPost("creation-options")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreationOptions()
    {
        var optionsJson = await _passkeyService.BeginRegister(CurrentUserId);
        return optionsJson is null
            ? NotFound()
            : Content(optionsJson, MediaTypeNames.Application.Json);
    }

    [HttpPost("register")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserActionResult))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<UserActionResult> Register([FromBody] RegisterPasskeyRequest request)
        => await _passkeyService.CompleteRegister(CurrentUserId, request.CredentialJson, request.Name);

    [HttpPost("rename")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserActionResult))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<UserActionResult> Rename([FromBody] RenamePasskeyRequest request)
        => await _passkeyService.Rename(CurrentUserId, request.CredentialId, request.Name);

    [HttpDelete("{credentialId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserActionResult))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<UserActionResult> Delete(string credentialId)
        => await _passkeyService.Delete(CurrentUserId, credentialId);

    [HttpPost("request-options")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestOptions()
        => Content(await _passkeyService.BeginLogin(), MediaTypeNames.Application.Json);

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthProtectionOption.RateLimitPolicyName)]
    [ProducesResponseType(typeof(AuthResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResultResponse>> Login([FromBody] PasskeyLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _passkeyService.Login(request.CredentialJson, cancellationToken);
        return result.IsAuthSuccessful
            ? Ok(result.ToResponse())
            : Unauthorized(result.ToResponse());
    }

    private Guid CurrentUserId => _requestContext.User!.Id;
}
