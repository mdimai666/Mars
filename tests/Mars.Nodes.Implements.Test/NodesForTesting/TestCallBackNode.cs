using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements;

namespace Mars.Nodes.Implements.Test.NodesForTesting;

public class TestCallBackNode : Node
{
    public delegate void CallBackNodeCallbackResult(NodeMsg input, ExecutionParameters parameters);

    public CallBackNodeCallbackResult? Callback;

    public TestCallBackNode()
    {
        Inputs = [new()];
        Outputs = [new()];
    }
}

public class TestCallBackNodeImpl : INodeImplement<TestCallBackNode>, INodeImplement
{
    public TestCallBackNodeImpl(TestCallBackNode node, IRED red)
    {
        Node = node;
        RED = red;
    }

    public TestCallBackNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        Node.Callback?.Invoke(input, parameters);

        return Task.CompletedTask;
    }

}
