using Mars.Core.Extensions;
using Mars.Host.Shared.QueryLang.Services;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Exceptions;
using Mars.Nodes.Core.Implements;
using Mars.WebApp.Nodes.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebApp.Nodes.Host.Nodes;

public class AppEntityReadNodeImpl : INodeImplement<AppEntityReadNode>, INodeImplement
{
    public AppEntityReadNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public AppEntityReadNodeImpl(AppEntityReadNode node, IRED _RED)
    {
        Node = node;
        RED = _RED;
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        if (Node.ExpressionInput == NodeExpressionInput.Builder && Node.Query.CallChains.None())
            throw new NodeExecuteException(Node, "Node.Query is empty");

        var expression = Node.ExpressionInput switch
        {
            NodeExpressionInput.String => Node.Expression,
            NodeExpressionInput.Builder => Node.Query.BuildString(),
            _ => throw new NotImplementedException($"Node.ExpressionInput '{Node.ExpressionInput}' is not implement")
        };

        var _queryLangLinqDatabaseQueryHandler = RED.ServiceProvider.GetRequiredService<IQueryLangLinqDatabaseQueryHandler>();

        var result = await _queryLangLinqDatabaseQueryHandler.Handle(expression, new(), parameters.CancellationToken);

        input.Payload = result;

        callback(input);
    }
}
