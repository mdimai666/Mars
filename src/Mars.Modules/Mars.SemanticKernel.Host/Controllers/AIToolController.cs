using System.Net.Mime;
using Mars.Core.Constants;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Host.Shared.Features;
using Mars.SemanticKernel.Host.Shared.Interfaces;
using Mars.SemanticKernel.Host.Shared.Mappings;
using Mars.SemanticKernel.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;

namespace Mars.SemanticKernel.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[NotFoundExceptionFilter]
[FluentValidationExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
[FeatureGate(FeatureFlags.AITool)]
public class AIToolController
{
    private readonly IMarsAIService _marsAIService;

    public AIToolController(IMarsAIService marsAIService)
    {
        _marsAIService = marsAIService;
    }

    [HttpPost("Prompt")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466)]
    public async Task<AIServiceResponse> Prompt(AIServiceRequest request, CancellationToken cancellationToken)
    {
        var reply = await _marsAIService.Reply(request.ToQuery().Prompt, cancellationToken: cancellationToken);

        return new AIServiceResponse { Content = reply };
    }

    [HttpGet("ConfigList")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IReadOnlyCollection<AIConfigNodeResponse> ConfigList()
    {
        return _marsAIService.ConfigList().ToResponse();
    }
}
