using System.ComponentModel;
using System.Net.Mime;
using Mars.Core.Constants;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Host.Shared.Features;
using Mars.SemanticKernel.Host.Shared.Interfaces;
using Mars.SemanticKernel.Host.Shared.Mappings;
using Mars.Shared.Common;
using Mars.Shared.Contracts.AIService;
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

    [HttpPost("ToolPrompt")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public async Task<AIServiceResponse> ToolPrompt([FromBody] AIServiceToolRequest request, CancellationToken cancellationToken)
    {
        var query = request.ToQuery();
        var reply = await _marsAIService.ReplyAsTool(query.Prompt, query.ToolName, useToolPresetSettings: true, cancellationToken: cancellationToken);

        return new AIServiceResponse { Content = reply };
    }

    [HttpGet("ToolScenarioList")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    public IReadOnlyCollection<string> ToolScenarioList([FromQuery][DefaultValue("[]")] string[]? tags = null)
    {
        return _marsAIService.ToolScenarioList(tags);
    }

}
