using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Host.Shared;
using static Mars.Nodes.Core.Nodes.CallNode;

namespace Mars.Nodes.Core.Implements.Nodes;

public class CallNodeImpl : INodeImplement<CallNode>
{
    public CallNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public CallNodeImpl(CallNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {

        callback(input);

        return Task.CompletedTask;
    }
}

public class CallResponseNodeImpl : INodeImplement<CallResponseNode>
{
    public CallResponseNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public CallResponseNodeImpl(CallResponseNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        CallNodeCallbackAction action = input.Get<CallNodeCallbackAction>()!;

        action.callback(input.Payload);

        return Task.CompletedTask;
    }
}
