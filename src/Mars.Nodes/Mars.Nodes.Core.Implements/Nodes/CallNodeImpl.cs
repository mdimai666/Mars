using Mars.Nodes.Core.Nodes;
using HandlebarsDotNet;
using static Mars.Nodes.Core.Nodes.CallNode;

namespace Mars.Nodes.Core.Implements.Nodes;

public class CallNodeImpl : INodeImplement<CallNode>, INodeImplement
{
    public CallNodeImpl(CallNode node, IRED RED)
    {
        this.Node = node;
        this.RED = RED;
    }

    public CallNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public Task Execute(NodeMsg input, ExecuteAction callback, Action<Exception> Error)
    {

        callback(input);

        return Task.CompletedTask;
    }
}

public class CallResponseNodeImpl : INodeImplement<CallResponseNode>, INodeImplement
{
    public CallResponseNodeImpl(CallResponseNode node, IRED RED)
    {
        this.Node = node;
        this.RED = RED;
    }

    public CallResponseNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public Task Execute(NodeMsg input, ExecuteAction callback, Action<Exception> Error)
    {
        CallNodeCallbackAction action = input.Get<CallNodeCallbackAction>()!;

        action.callback(input.Payload);

        return Task.CompletedTask;
    }
}