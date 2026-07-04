using Mars.Nodes.Host.Shared;

namespace Mars.Nodes.Core.Implements.Nodes.Common;

public class CommentNodeImpl : INodeImplement<CommentNode>
{
    public CommentNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public CommentNodeImpl(CommentNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        throw new NotSupportedException();
    }
}
