using System.Text.RegularExpressions;
using Mars.CodeCompletion.Contracts.Dto;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Tags;
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
        using var lease = await workspaceManager.GetDocumentAsync(contextId, request.DocumentId, request.Code, ct);
        var document = lease.Document;

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

            var capped = filtered.Take(MaxCompletionItems + 1).ToList();
            var incomplete = capped.Count > MaxCompletionItems;
            if (incomplete)
                capped.RemoveAt(capped.Count - 1);

            var imports = GetEffectiveImports(document, request.Code);
            CompletionTypeIndex? typeIndex = null;

            var items = new List<CompletionItemDto>(capped.Count);
            foreach (var item in capped)
            {
                var dto = new CompletionItemDto
                {
                    Label = item.DisplayText,
                    Kind = MonacoCompletionKinds.FromRoslynTags(item.Tags),
                    InsertText = item.DisplayText,
                    FilterText = item.FilterText,
                    SortText = item.SortText,
                };

                if (IsTypeItem(item))
                {
                    typeIndex ??= await workspaceManager.GetTypeIndexAsync(contextId, document, ct);
                    var usingEdit = TryBuildUsingEdit(item, typeIndex, imports);
                    if (usingEdit != null)
                        dto.AdditionalTextEdits = [usingEdit];
                }

                items.Add(dto);
            }

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

    private static bool IsTypeItem(CompletionItem item)
        => item.Tags.Contains(WellKnownTags.Class)
            || item.Tags.Contains(WellKnownTags.Interface)
            || item.Tags.Contains(WellKnownTags.Structure)
            || item.Tags.Contains(WellKnownTags.Enum)
            || item.Tags.Contains(WellKnownTags.Delegate);

    /// <summary>
    /// VS-поведение для неимпортированных типов: вместе с вставкой имени дописать `using`.
    /// Правка создаётся только когда имя однозначно (ровно один namespace во всех ссылках)
    /// и он ещё не импортирован — иначе не угадываем.
    /// </summary>
    private static AdditionalTextEditDto? TryBuildUsingEdit(
        CompletionItem item, CompletionTypeIndex typeIndex, HashSet<string> imports)
    {
        var name = item.DisplayText;
        var generic = name.IndexOf('<');
        if (generic >= 0)
            name = name[..generic];

        var namespaces = typeIndex.GetNamespaces(name);
        if (namespaces.Count != 1 || imports.Contains(namespaces[0]))
            return null;

        return new AdditionalTextEditDto
        {
            OffsetFrom = 0,
            OffsetTo = 0,
            NewText = $"using {namespaces[0]};\n",
        };
    }

    private static HashSet<string> GetEffectiveImports(Document document, string code)
    {
        var imports = new HashSet<string>(StringComparer.Ordinal);

        if (document.Project.CompilationOptions is CSharpCompilationOptions options)
        {
            foreach (var usingDirective in options.Usings)
                imports.Add(usingDirective);
        }

        foreach (Match match in Regex.Matches(code, @"^[ \t]*using[ \t]+([A-Za-z_][\w.]*)[ \t]*;", RegexOptions.Multiline))
            imports.Add(match.Groups[1].Value);

        return imports;
    }
}
