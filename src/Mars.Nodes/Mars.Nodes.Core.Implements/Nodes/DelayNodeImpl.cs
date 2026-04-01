using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes;

public class DelayNodeImpl : INodeImplement<DelayNode>, INodeImplement
{
    public DelayNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public DelayNodeImpl(DelayNode node, IRED red)
    {
        Node = node;
        RED = red;
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        await Task.Delay(Node.DelayMillis, cancellationToken: parameters.CancellationToken);

        callback(input);

    }
}
