using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mars.CodeCompletion.Host.Services;

/// <summary>
/// Находит вызов (метод/конструктор/атрибут), в списке аргументов которого стоит курсор.
/// SignatureHelpService в Roslyn internal, поэтому signature help строится вручную по SemanticModel.
/// </summary>
internal sealed class InvocationContext
{
    public static async Task<InvocationContext?> GetInvocationAsync(Document document, int position, CancellationToken ct)
    {
        var tree = await document.GetSyntaxTreeAsync(ct);
        if (tree == null)
            return null;

        var root = await tree.GetRootAsync(ct);
        var node = root.FindToken(position).Parent;

        while (node != null)
        {
            if (node is InvocationExpressionSyntax invocation && invocation.ArgumentList.Span.Contains(position))
            {
                var model = await document.GetSemanticModelAsync(ct);
                return model == null ? null : new InvocationContext(model, position, invocation.Expression, invocation.ArgumentList);
            }

            if (node is BaseObjectCreationExpressionSyntax objectCreation && (objectCreation.ArgumentList?.Span.Contains(position) ?? false))
            {
                var model = await document.GetSemanticModelAsync(ct);
                return model == null ? null : new InvocationContext(model, position, objectCreation, objectCreation.ArgumentList);
            }

            if (node is AttributeSyntax attributeSyntax && (attributeSyntax.ArgumentList?.Span.Contains(position) ?? false))
            {
                var model = await document.GetSemanticModelAsync(ct);
                return model == null ? null : new InvocationContext(model, position, attributeSyntax, attributeSyntax.ArgumentList);
            }

            node = node.Parent;
        }

        return null;
    }

    public SemanticModel SemanticModel { get; }
    public int Position { get; }
    public SyntaxNode Receiver { get; }
    public IReadOnlyList<TypeInfo> ArgumentTypes { get; }
    public IEnumerable<SyntaxToken> Separators { get; }

    private InvocationContext(SemanticModel semanticModel, int position, SyntaxNode receiver, ArgumentListSyntax argumentList)
    {
        SemanticModel = semanticModel;
        Position = position;
        Receiver = receiver;
        ArgumentTypes = argumentList.Arguments.Select(a => semanticModel.GetTypeInfo(a.Expression)).ToList();
        Separators = argumentList.Arguments.GetSeparators();
    }

    private InvocationContext(SemanticModel semanticModel, int position, SyntaxNode receiver, AttributeArgumentListSyntax argumentList)
    {
        SemanticModel = semanticModel;
        Position = position;
        Receiver = receiver;
        ArgumentTypes = argumentList.Arguments.Select(a => semanticModel.GetTypeInfo(a.Expression)).ToList();
        Separators = argumentList.Arguments.GetSeparators();
    }
}
