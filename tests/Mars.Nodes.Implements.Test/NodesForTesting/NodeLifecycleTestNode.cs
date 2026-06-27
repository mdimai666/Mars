using Mars.Nodes.Core;
using Mars.Nodes.Host.Shared;

namespace Mars.Nodes.Implements.Test.NodesForTesting;

public class NodeLifecycleTestNode : Node
{
    public NodeLifecycleTestNode()
    {
        Inputs = [new()];
        Outputs = [new()];
    }
}

public class NodeLifecycleTestNodeImpl : INodeImplement<NodeLifecycleTestNode>, INodeLifecycleOnAssigned, INodeLifecycleOnDelete, IDisposable, IAsyncDisposable
{
    public NodeLifecycleTestNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public NodeLifecycleTestNodeImpl(NodeLifecycleTestNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public virtual Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnNodeAssigned(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnNodeDelete()
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
