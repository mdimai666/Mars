using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes;

public class UnknownNodeImpl : INodeImplement<UnknownNode>, INodeImplement
{
    public UnknownNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public UnknownNodeImpl(UnknownNode node, IRED red)
    {
        Node = node;
        RED = red;
    }

    public Task Execute(NodeMsg _, ExecuteAction callback)
    {
        throw new NotImplementedException();
    }
}
