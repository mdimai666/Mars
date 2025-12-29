using Mars.Core.Extensions;
using Mars.Host.Shared.QueryLang.Services;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Exceptions;
using Mars.Nodes.Core.Implements;
using Mars.Shared.Models;
using Mars.WebApp.Nodes.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebApp.Nodes.Host.Nodes;

public class AppEntityReadNodeImpl : INodeImplement<AppEntityReadNode>, INodeImplement
{
    private readonly IMetaModelTypesLocator _metaModelTypesLocator;

    public AppEntityReadNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public AppEntityReadNodeImpl(AppEntityReadNode node, IRED _RED, IMetaModelTypesLocator metaModelTypesLocator)
    {
        Node = node;
        RED = _RED;
        _metaModelTypesLocator = metaModelTypesLocator;
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        if (Node.ExpressionInput == NodeExpressionInput.String)
        {
            if (string.IsNullOrEmpty(Node.Expression))
                throw new NodeExecuteException(Node, "Node.Expression is empty");
            var expression = Node.Expression;
            var queryLang = RED.ServiceProvider.GetRequiredService<IQueryLangLinqDatabaseQueryHandler>();
            var result = await queryLang.Handle(expression, new(), parameters.CancellationToken);

            input.Payload = result;

            var resolveResult = _metaModelTypesLocator.ResolveEntityNameToSourceUri(expression.Split('.')[0]);
            var requestInfo = new AppEntityReadRequestInfo { Expression = expression, EntityUri = resolveResult.EntityUri };
            input.Add(requestInfo);

            callback(input);
        }
        else if (Node.ExpressionInput == NodeExpressionInput.Builder)
        {
            if (Node.Query.CallChains.None())
                throw new NodeExecuteException(Node, "Node.Query is empty");
            var expression = Node.Query.BuildString();
            var queryLang = RED.ServiceProvider.GetRequiredService<IQueryLangLinqDatabaseQueryHandler>();
            var result = await queryLang.Handle(expression, new(), parameters.CancellationToken);

            input.Payload = result;

            var resolveResult = _metaModelTypesLocator.ResolveEntityNameToSourceUri(Node.Query.EntityName);
            var requestInfo = new AppEntityReadRequestInfo { Expression = expression, EntityUri = resolveResult.EntityUri };
            input.Add(requestInfo);

            callback(input);
        }
        else
        {
            throw new NotImplementedException($"Node.ExpressionInput '{Node.ExpressionInput}' is not implement");
        }
    }
}

public record AppEntityReadRequestInfo
{
    public required string Expression { get; init; }
    public required SourceUri EntityUri { get; init; }

}
