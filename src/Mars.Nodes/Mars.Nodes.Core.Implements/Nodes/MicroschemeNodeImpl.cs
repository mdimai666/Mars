using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes;

public class MicroschemeNodeImpl : INodeImplement<MicroschemeNode>, INodeImplement
{

    public MicroschemeNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public MicroschemeNodeImpl(MicroschemeNode node, IRED RED)
    {
        this.Node = node;
        this.RED = RED;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, Action<Exception> Error)
    {

        return Task.CompletedTask;
    }
}
