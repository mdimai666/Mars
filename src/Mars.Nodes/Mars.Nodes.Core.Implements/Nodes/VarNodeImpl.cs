using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes;

public class VarNodeImpl : INodeImplement<VarNode>, INodeImplement
{
    public VarNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public VarNodeImpl(VarNode node, IRED RED)
    {
        this.Node = node;
        this.RED = RED;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback)
    {
        throw new NotSupportedException("VarNode not executable");
    }
}
