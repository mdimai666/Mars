using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes;

public class DelayNodeImpl : INodeImplement<DelayNode>, INodeImplement
{
    public DelayNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public DelayNodeImpl(DelayNode node, IRED RED)
    {
        this.Node = node;
        this.RED = RED;
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        await Task.Delay(Node.DelayMillis);

        callback(input);

    }
}
