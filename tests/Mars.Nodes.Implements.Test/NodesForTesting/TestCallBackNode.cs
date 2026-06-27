using Mars.Nodes.Core;
using Mars.Nodes.Host.Shared;

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

public class TestCallBackNodeImpl : INodeImplement<TestCallBackNode>
{
    public TestCallBackNodeImpl(TestCallBackNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public TestCallBackNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        Node.Callback?.Invoke(input, parameters);

        callback(input);

        return Task.CompletedTask;
    }

}
