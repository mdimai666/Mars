using Mars.CodeCompletion.Contracts.Dto;
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

        var completions = await service.GetCompletionsAsync(document, request.Offset, cancellationToken: ct);
        if (completions == null)
            return new CompletionResponseDto();

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
}
