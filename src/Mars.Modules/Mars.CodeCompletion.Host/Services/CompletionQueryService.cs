using Mars.CodeCompletion.Contracts.Dto;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.Extensions.Logging;

namespace Mars.CodeCompletion.Host.Services;

public class CompletionQueryService(
    CodeCompletionWorkspaceManager workspaceManager,
    ILogger<CompletionQueryService> logger)
{
    public async Task<CompletionResponseDto> GetCompletionsAsync(string contextId, CodePositionRequest request, CancellationToken ct)
    {
        var document = await workspaceManager.GetDocumentAsync(contextId, request.DocumentId, request.Code, ct);

        var service = CompletionService.GetService(document);
        if (service == null)
        {
            logger.LogWarning("CompletionService is unavailable (MEF host lacks Features assemblies?)");
            return new CompletionResponseDto();
        }

        try
        {
            var completions = await service.GetCompletionsAsync(document, request.Offset, cancellationToken: ct);
            if (completions == null)
            {
                var compilation = await document.Project.GetCompilationAsync(ct);
                logger.LogWarning(
                    "GetCompletionsAsync returned null for context '{ContextId}' (compilation null: {CompilationNull}, refs: {Refs}, errors: {Errors})",
                    contextId,
                    compilation == null,
                    compilation?.References.Count() ?? -1,
                    compilation == null ? -1 : compilation.GetDiagnostics(ct).Count(d => d.Severity == DiagnosticSeverity.Error));
                return new CompletionResponseDto();
            }

            var items = completions.ItemsList.Select(item => new CompletionItemDto
            {
                Label = item.DisplayText,
                Kind = MonacoCompletionKinds.FromRoslynTags(item.Tags),
                InsertText = item.DisplayText,
                FilterText = item.FilterText,
                SortText = item.SortText,
            }).ToList();

            return new CompletionResponseDto { Items = items };
        }
        catch (Exception e)
        {
            logger.LogError(e, "Code completion failed for context '{ContextId}'", contextId);
            return new CompletionResponseDto();
        }
    }
}
