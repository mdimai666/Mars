using Mars.CodeCompletion.Contracts.Dto;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;

namespace Mars.CodeCompletion.Host.Services;

/// <summary>
/// Семантические токены для monaco: Classifier.GetClassifiedSpans → легенда LSP-типов
/// → delta-кодирование [deltaLine, deltaStartChar, length, tokenType, tokenModifiers].
/// Маппинг классификаций — схема OmniSharp SemanticTokensFeature (в альфе лежала в MySemantic.cs).
/// </summary>
public class SemanticTokensQueryService(
    CodeCompletionWorkspaceManager workspaceManager,
    ILogger<SemanticTokensQueryService> logger)
{
    // ДЕРЖАТЬ В СИНХРОНЕ с легендой в codeCompletion.js (порядок = индексы tokenType)
    public static readonly string[] TokenTypes =
    [
        "namespace", "class", "enum", "interface", "struct", "typeParameter",
        "parameter", "variable", "property", "enumMember", "event", "method",
        "keyword", "string", "comment", "number",
    ];

    private static readonly Dictionary<string, int> ClassificationMap = BuildMap();

    private static Dictionary<string, int> BuildMap()
    {
        int Index(string tokenType) => Array.IndexOf(TokenTypes, tokenType);

        var map = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [ClassificationTypeNames.NamespaceName] = Index("namespace"),
            [ClassificationTypeNames.ClassName] = Index("class"),
            [ClassificationTypeNames.RecordClassName] = Index("class"),
            [ClassificationTypeNames.DelegateName] = Index("class"),
            [ClassificationTypeNames.EnumName] = Index("enum"),
            [ClassificationTypeNames.InterfaceName] = Index("interface"),
            [ClassificationTypeNames.StructName] = Index("struct"),
            [ClassificationTypeNames.RecordStructName] = Index("struct"),
            [ClassificationTypeNames.TypeParameterName] = Index("typeParameter"),
            [ClassificationTypeNames.ParameterName] = Index("parameter"),
            [ClassificationTypeNames.LocalName] = Index("variable"),
            [ClassificationTypeNames.FieldName] = Index("property"),
            [ClassificationTypeNames.PropertyName] = Index("property"),
            [ClassificationTypeNames.EnumMemberName] = Index("enumMember"),
            [ClassificationTypeNames.ConstantName] = Index("enumMember"),
            [ClassificationTypeNames.EventName] = Index("event"),
            [ClassificationTypeNames.MethodName] = Index("method"),
            [ClassificationTypeNames.ExtensionMethodName] = Index("method"),
            [ClassificationTypeNames.Keyword] = Index("keyword"),
            [ClassificationTypeNames.ControlKeyword] = Index("keyword"),
            [ClassificationTypeNames.StringLiteral] = Index("string"),
            [ClassificationTypeNames.VerbatimStringLiteral] = Index("string"),
            [ClassificationTypeNames.Comment] = Index("comment"),
            [ClassificationTypeNames.NumericLiteral] = Index("number"),
        };

        // XML-doc — тоже комментарий
        foreach (var name in new[]
                 {
                     ClassificationTypeNames.XmlDocCommentText,
                     ClassificationTypeNames.XmlDocCommentName,
                     ClassificationTypeNames.XmlDocCommentDelimiter,
                     ClassificationTypeNames.XmlDocCommentComment,
                     ClassificationTypeNames.XmlDocCommentAttributeName,
                     ClassificationTypeNames.XmlDocCommentAttributeValue,
                     ClassificationTypeNames.XmlDocCommentAttributeQuotes,
                     ClassificationTypeNames.XmlDocCommentCDataSection,
                     ClassificationTypeNames.XmlDocCommentEntityReference,
                     ClassificationTypeNames.XmlDocCommentProcessingInstruction,
                 })
            map[name] = Index("comment");

        return map;
    }

    public async Task<IReadOnlyList<int>> GetSemanticTokensDataAsync(
        string contextId, CodePositionRequest request, CancellationToken ct)
    {
        var document = await workspaceManager.GetDocumentAsync(contextId, request.DocumentId, request.Code, ct);

        try
        {
            var text = await document.GetTextAsync(ct);
            var spans = await Classifier.GetClassifiedSpansAsync(
                document, new TextSpan(0, text.Length), cancellationToken: ct);

            var data = new List<int>();
            var prevLine = 0;
            var prevChar = 0;
            var prevEnd = -1;

            foreach (var span in spans.OrderBy(s => s.TextSpan.Start))
            {
                if (!ClassificationMap.TryGetValue(span.ClassificationType, out var tokenType))
                    continue;

                // overlappingTokenSupport у monaco false (StaticSymbol/StringEscape и т.п.
                // в карту не входят, но XML-doc спаны могут перекрываться) — пропускаем наложения
                if (span.TextSpan.Length == 0 || span.TextSpan.Start < prevEnd)
                    continue;
                prevEnd = span.TextSpan.End;

                var pos = text.Lines.GetLinePosition(span.TextSpan.Start);
                var deltaLine = pos.Line - prevLine;
                data.Add(deltaLine);
                data.Add(deltaLine == 0 ? pos.Character - prevChar : pos.Character);
                data.Add(span.TextSpan.Length);
                data.Add(tokenType);
                data.Add(0); // модификаторы (static/declaration) — фаза 2

                prevLine = pos.Line;
                prevChar = pos.Character;
            }

            return data;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Semantic tokens failed for context '{ContextId}'", contextId);
            return [];
        }
    }
}
