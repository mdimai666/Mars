using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Host.Shared;

namespace Mars.Nodes.Core.Implements.Nodes;

public class VarNodeImpl : INodeImplement<VarNode>
{
    public VarNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public VarNodeImpl(VarNode node, IRuntimeNodeScope rns)
    {
        this.Node = node;
        this.RNS = rns;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        throw new NotSupportedException("VarNode not executable");
    }
}
