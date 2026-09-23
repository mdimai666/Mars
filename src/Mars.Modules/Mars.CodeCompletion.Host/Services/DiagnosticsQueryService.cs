using Mars.CodeCompletion.Contracts.Dto;
using Microsoft.CodeAnalysis;

namespace Mars.CodeCompletion.Host.Services;

public class DiagnosticsQueryService(CodeCompletionWorkspaceManager workspaceManager)
{
    public async Task<IReadOnlyList<DiagnosticDto>> GetDiagnosticsAsync(string contextId, CodePositionRequest request, CancellationToken ct)
    {
        var document = await workspaceManager.GetDocumentAsync(contextId, request.DocumentId, request.Code, ct);

        var tree = await document.GetSyntaxTreeAsync(ct);
        if (tree == null)
            return [];

        var compilation = await document.Project.GetCompilationAsync(ct);
        if (compilation == null)
            return [];

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

    // Monaco MarkerSeverity
    private static int ToMonacoSeverity(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Error => 8,
        DiagnosticSeverity.Warning => 4,
        DiagnosticSeverity.Info => 2,
        _ => 1,
    };
}
