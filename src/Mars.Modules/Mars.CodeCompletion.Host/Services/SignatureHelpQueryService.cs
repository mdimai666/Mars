using Mars.CodeCompletion.Contracts.Dto;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mars.CodeCompletion.Host.Services;

public class SignatureHelpQueryService(CodeCompletionWorkspaceManager workspaceManager)
{
    public async Task<SignatureHelpResponseDto?> GetSignatureHelpAsync(string contextId, CodePositionRequest request, CancellationToken ct)
    {
        var document = await workspaceManager.GetDocumentAsync(contextId, request.DocumentId, request.Code, ct);
        var invocation = await InvocationContext.GetInvocationAsync(document, request.Offset, ct);
        if (invocation == null)
            return null;

        var activeParameter = 0;
        foreach (var comma in invocation.Separators)
        {
            if (comma.Span.Start > invocation.Position)
                break;
            activeParameter += 1;
        }

        var semanticModel = invocation.SemanticModel;
        var methodGroup = semanticModel.GetMemberGroup(invocation.Receiver).OfType<IMethodSymbol>().ToList();

        if (invocation.Receiver is MemberAccessExpressionSyntax memberAccess)
        {
            var throughExpression = memberAccess.Expression;
            var throughType = semanticModel.GetTypeInfo(throughExpression, ct).Type;
            var throughSymbol = semanticModel.GetSymbolInfo(throughExpression, ct).Symbol;

            var includeStatic = throughSymbol is ITypeSymbol || throughType != null;
            var includeInstance = (throughSymbol != null && throughSymbol is not ITypeSymbol)
                || throughExpression is LiteralExpressionSyntax
                || throughExpression is TypeOfExpressionSyntax
                || throughType != null;

            methodGroup = methodGroup
                .Where(m => (m.IsStatic && includeStatic) || (!m.IsStatic && includeInstance))
                .ToList();
        }

        if (methodGroup.Count == 0)
            return null;

        var signatures = new List<SignatureDto>();
        var methodsByLabel = new Dictionary<string, IMethodSymbol>(StringComparer.Ordinal);
        var bestScore = int.MinValue;
        var bestLabel = "";

        foreach (var method in methodGroup)
        {
            var dto = BuildSignature(method);
            if (!methodsByLabel.TryAdd(dto.Label, method))
                continue;

            var score = InvocationScore(method, invocation.ArgumentTypes);
            if (score > bestScore)
            {
                bestScore = score;
                bestLabel = dto.Label;
            }

            signatures.Add(dto);
        }

        return new SignatureHelpResponseDto
        {
            Signatures = signatures,
            ActiveSignature = signatures.FindIndex(s => s.Label == bestLabel),
            ActiveParameter = activeParameter,
        };
    }

    private static SignatureDto BuildSignature(IMethodSymbol symbol)
    {
        return new SignatureDto
        {
            Label = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            Documentation = symbol.GetDocumentationCommentXml(),
            Parameters = symbol.Parameters
                .Select(p => new SignatureParameterDto
                {
                    Label = p.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    Documentation = p.GetDocumentationCommentXml(),
                })
                .ToList(),
        };
    }

    private static int InvocationScore(IMethodSymbol symbol, IReadOnlyList<TypeInfo> argumentTypes)
    {
        var parameters = symbol.Parameters;
        if (!parameters.Any(p => p.IsParams) && parameters.Length < argumentTypes.Count)
            return int.MinValue;

        var score = 0;
        for (var i = 0; i < parameters.Length && i < argumentTypes.Count; i++)
        {
            var converted = argumentTypes[i].ConvertedType;
            if (converted == null)
                score += 1;
            else if (SymbolEqualityComparer.Default.Equals(converted, parameters[i].Type))
                score += 2;
        }

        return score;
    }
}
