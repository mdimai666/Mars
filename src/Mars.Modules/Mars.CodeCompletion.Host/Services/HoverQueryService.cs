using System.Text;
using Mars.CodeCompletion.Contracts.Dto;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.Extensions.Logging;

namespace Mars.CodeCompletion.Host.Services;

public class HoverQueryService(
    CodeCompletionWorkspaceManager workspaceManager,
    ILogger<HoverQueryService> logger)
{
    public async Task<HoverResponseDto?> GetHoverAsync(string contextId, CodePositionRequest request, CancellationToken ct)
    {
        using var lease = await workspaceManager.GetDocumentAsync(contextId, request.DocumentId, request.Code, ct);
        var document = lease.Document;

        var service = QuickInfoService.GetService(document);
        if (service == null)
        {
            logger.LogWarning("QuickInfoService is unavailable (MEF host lacks Features assemblies?)");
            return null;
        }

        var info = await service.GetQuickInfoAsync(document, request.Offset, ct);
        if (info == null)
            return null;

        var content = new StringBuilder();
        var sections = info.Sections.ToList();
        for (var i = 0; i < sections.Count; i++)
        {
            var section = sections[i];
            var text = string.Concat(section.TaggedParts.Select(p => p.Text));
            if (string.IsNullOrWhiteSpace(text))
                continue;

            if (content.Length > 0)
                content.Append("\n\n");

            // первая секция — объявление символа, остальное (документация и т.п.) — текст
            content.Append(i == 0 ? $"```csharp\n{text}\n```" : text);
        }

        if (content.Length == 0)
            return null;

        return new HoverResponseDto
        {
            Content = content.ToString(),
            OffsetFrom = info.Span.Start,
            OffsetTo = info.Span.End,
        };
    }
}
