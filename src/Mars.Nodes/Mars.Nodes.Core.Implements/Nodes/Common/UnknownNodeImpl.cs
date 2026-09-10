using Mars.Nodes.Abstractions;
using Mars.Nodes.Core.Exceptions;

namespace Mars.Nodes.Core.Implements.Nodes.Common;

public class UnknownNodeImpl : INodeImplement<UnknownNode>
{
    public UnknownNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public UnknownNodeImpl(UnknownNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public Task Execute(NodeMsg _, ExecuteAction callback, ExecutionParameters parameters)
    {
        throw new NodeExecuteException(Node, $"UnknownNode: '{Node.UnrecognizedType}' not found");
    }
}
