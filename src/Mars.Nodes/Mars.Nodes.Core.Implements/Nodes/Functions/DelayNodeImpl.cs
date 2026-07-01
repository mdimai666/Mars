using Mars.Nodes.Core.Nodes.Functions;
using Mars.Nodes.Host.Shared;

namespace Mars.Nodes.Core.Implements.Nodes.Functions;

public class DelayNodeImpl : INodeImplement<DelayNode>
{
    public DelayNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public DelayNodeImpl(DelayNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        await Task.Delay(Node.DelayMillis, cancellationToken: parameters.CancellationToken);

        parameters.CancellationToken.ThrowIfCancellationRequested();
        callback(input);

    }
}
