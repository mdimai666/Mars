using Mars.CodeCompletion.Contracts.Dto;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Mars.CodeCompletion.Host.Services;

public class DiagnosticsQueryService(
    CodeCompletionWorkspaceManager workspaceManager,
    ILogger<DiagnosticsQueryService> logger)
{
    public async Task<IReadOnlyList<DiagnosticDto>> GetDiagnosticsAsync(string contextId, CodePositionRequest request, CancellationToken ct)
    {
        var document = await workspaceManager.GetDocumentAsync(contextId, request.DocumentId, request.Code, ct);

        try
        {
            var tree = await document.GetSyntaxTreeAsync(ct);
            if (tree == null)
                return [];

            var compilation = await document.Project.GetCompilationAsync(ct);
            if (compilation == null)
            {
                logger.LogWarning("Compilation is null for context '{ContextId}' project '{Project}'", contextId, document.Project.Name);
                return [];
            }

            return compilation.GetDiagnostics(ct)
                .Where(d => d.Location.SourceTree == tree && !d.IsSuppressed)
                .Select(d =>
                {
                    var span = d.Location.SourceSpan;
                    return new DiagnosticDto
                    {
                        OffsetFrom = span.Start,
                        OffsetTo = span.End,
                        Severity = ToMonacoSeverity(d.Severity),
                        Message = d.GetMessage(),
                        Id = d.Id,
                    };
                })
                .ToList();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Code completion diagnostics failed for context '{ContextId}'", contextId);
            return [];
        }
    }

    // Monaco MarkerSeverity
    private static int ToMonacoSeverity(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Error => 8,
        DiagnosticSeverity.Warning => 4,
        DiagnosticSeverity.Info => 2,
        _ => 1,
    };
}
