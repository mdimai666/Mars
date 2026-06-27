using Mars.Nodes.Core;
using Mars.Nodes.Host.Shared;

namespace Mars.Integration.Tests.Nodes;

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
    public TestCallBackNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public TestCallBackNodeImpl(TestCallBackNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        Node.Callback?.Invoke(input, parameters);

        callback(input);

        return Task.CompletedTask;
    }

}
