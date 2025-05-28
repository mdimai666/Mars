using Mars.Nodes.Core.Nodes;
using static Mars.Nodes.Core.Nodes.CallNode;

namespace Mars.Nodes.Core.Implements.Nodes;

public class CallNodeImpl : INodeImplement<CallNode>, INodeImplement
{
    public CallNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public CallNodeImpl(CallNode node, IRED RED)
    {
        this.Node = node;
        this.RED = RED;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback)
    {

        callback(input);

        return Task.CompletedTask;
    }
}

public class CallResponseNodeImpl : INodeImplement<CallResponseNode>, INodeImplement
{
    public CallResponseNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public CallResponseNodeImpl(CallResponseNode node, IRED RED)
    {
        this.Node = node;
        this.RED = RED;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback)
    {
        CallNodeCallbackAction action = input.Get<CallNodeCallbackAction>()!;

        action.callback(input.Payload);

        return Task.CompletedTask;
    }
}
