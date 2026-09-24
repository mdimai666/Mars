using System.Net.Mime;
using Mars.CodeCompletion.Contracts.Dto;
using Mars.CodeCompletion.Host.Services;
using Mars.Server.Abstractions.ExceptionFilters;
using Mars.Server.Abstractions.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;

namespace Mars.CodeCompletion.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[NotFoundExceptionFilter]
[FluentValidationExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
[FeatureGate(FeatureFlags.CodeCompletion)]
public class CodeCompletionController(
    CodeCompletionWorkspaceManager workspaceManager,
    CompletionQueryService completionService,
    HoverQueryService hoverService,
    SignatureHelpQueryService signatureHelpService,
    DiagnosticsQueryService diagnosticsService,
    SemanticTokensQueryService semanticTokensService) : ControllerBase
{
    [HttpGet("info")]
    public CodeCompletionInfo GetInfo()
        => new()
        {
            Enabled = true,
            Contexts = workspaceManager.ContextIds,
        };

    [HttpPost("{contextId}/completion")]
    public Task<CompletionResponseDto> Completion(string contextId, CodePositionRequest request, CancellationToken ct)
        => completionService.GetCompletionsAsync(contextId, request, ct);

    [HttpPost("{contextId}/hover")]
    public async Task<IActionResult> Hover(string contextId, CodePositionRequest request, CancellationToken ct)
        => await hoverService.GetHoverAsync(contextId, request, ct) is { } hover ? Ok(hover) : NoContent();

    [HttpPost("{contextId}/signature")]
    public async Task<IActionResult> SignatureHelp(string contextId, CodePositionRequest request, CancellationToken ct)
        => await signatureHelpService.GetSignatureHelpAsync(contextId, request, ct) is { } help ? Ok(help) : NoContent();

    [HttpPost("{contextId}/diagnostics")]
    public Task<IReadOnlyList<DiagnosticDto>> Diagnostics(string contextId, CodePositionRequest request, CancellationToken ct)
        => diagnosticsService.GetDiagnosticsAsync(contextId, request, ct);

    [HttpPost("{contextId}/analyze")]
    public async Task<AnalyzeResponseDto> Analyze(string contextId, CodePositionRequest request, CancellationToken ct)
        => new()
        {
            Diagnostics = await diagnosticsService.GetDiagnosticsAsync(contextId, request, ct),
            SemanticTokensData = await semanticTokensService.GetSemanticTokensDataAsync(contextId, request, ct),
        };

    [HttpDelete("{contextId}/document/{documentId}")]
    public void RemoveDocument(string contextId, string documentId)
        => workspaceManager.RemoveDocument(contextId, documentId);
}
