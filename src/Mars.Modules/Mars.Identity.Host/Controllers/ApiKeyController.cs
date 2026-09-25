using System.Net.Mime;
using Mars.Contracts.Common;
using Mars.Core.Constants;
using Mars.Identity.Abstractions.Interfaces;
using Mars.Identity.Abstractions.Mappings.ApiKeys;
using Mars.Identity.Abstractions.Services;
using Mars.Identity.Contracts.ApiKeys;
using Mars.Server.Abstractions.ExceptionFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Identity.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[NotFoundExceptionFilter]
[FluentValidationExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
public class ApiKeyController : ControllerBase
{
    private readonly IApiKeyService _apiKeyService;
    private readonly IRequestContext _requestContext;

    public ApiKeyController(IApiKeyService apiKeyService, IRequestContext requestContext)
    {
        _apiKeyService = apiKeyService;
        _requestContext = requestContext;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IReadOnlyCollection<ApiKeySummaryResponse>> List(CancellationToken cancellationToken)
    {
        var keys = await _apiKeyService.ListByUser(CurrentUserId, cancellationToken);
        return keys.Select(s => s.ToResponse()).ToList();
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserActionResult<CreatedApiKeyResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public async Task<UserActionResult<CreatedApiKeyResponse>> Create([FromBody] CreateApiKeyRequest request, CancellationToken cancellationToken)
    {
        var result = await _apiKeyService.Create(request.ToQuery(CurrentUserId), cancellationToken);

        return result.Ok
            ? UserActionResult<CreatedApiKeyResponse>.Success(result.Data.ToResponse())
            : UserActionResult<CreatedApiKeyResponse>.Exception(result.Message);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserActionResult))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public async Task<UserActionResult> Revoke(Guid id, CancellationToken cancellationToken)
    {
        return await _apiKeyService.Revoke(id, CurrentUserId, cancellationToken);
    }

    private Guid CurrentUserId => _requestContext.User!.Id;
}
