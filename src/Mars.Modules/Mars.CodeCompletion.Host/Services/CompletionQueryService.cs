using Mars.CodeCompletion.Contracts.Dto;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.Extensions.Logging;

namespace Mars.CodeCompletion.Host.Services;

public class CompletionQueryService(
    CodeCompletionWorkspaceManager workspaceManager,
    ILogger<CompletionQueryService> logger)
{
    // как clangd --limit-results: топ-N по релевантности + isIncomplete, дозапрос по мере ввода
    private const int MaxCompletionItems = 200;

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

            // Roslyn отдаёт ItemsList неотсортированным и нефильтрованным (этим занимается клиент),
            // но при серверном капе (схема clangd --limit-results / OmniSharp) сортировку и
            // фильтрацию по набранному слову нужно сделать здесь, иначе кап срежет релевантное.
            var prefix = GetWordPrefix(request.Code, request.Offset);
            var filtered = completions.ItemsList
                .Where(item => prefix.Length == 0
                    || (item.FilterText ?? item.DisplayText).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.SortText, StringComparer.Ordinal)
                .ThenBy(item => item.FilterText ?? item.DisplayText, StringComparer.Ordinal);

            var items = filtered.Take(MaxCompletionItems + 1).Select(item => new CompletionItemDto
            {
                Label = item.DisplayText,
                Kind = MonacoCompletionKinds.FromRoslynTags(item.Tags),
                InsertText = item.DisplayText,
                FilterText = item.FilterText,
                SortText = item.SortText,
            }).ToList();

            var incomplete = items.Count > MaxCompletionItems;
            if (incomplete)
                items.RemoveAt(items.Count - 1);

            return new CompletionResponseDto { Items = items, Incomplete = incomplete };
        }
        catch (Exception e)
        {
            logger.LogError(e, "Code completion failed for context '{ContextId}'", contextId);
            return new CompletionResponseDto();
        }
    }

    private static string GetWordPrefix(string code, int offset)
    {
        var end = Math.Clamp(offset, 0, code.Length);
        var start = end;
        while (start > 0 && (char.IsLetterOrDigit(code[start - 1]) || code[start - 1] == '_'))
            start--;
        return code[start..end];
    }
}
