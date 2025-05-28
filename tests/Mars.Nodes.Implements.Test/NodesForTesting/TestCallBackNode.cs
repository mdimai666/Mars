using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements;

namespace Mars.Nodes.Implements.Test.NodesForTesting;

public class TestCallBackNode : Node
{
    public Action? Callback;
}

public class TestCallBackNodeImpl : INodeImplement<TestCallBackNode>, INodeImplement
{
    public TestCallBackNodeImpl(TestCallBackNode node, IRED RED)
    {
        this.Node = node;
        this.RED = RED;
    }

    public TestCallBackNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public Task Execute(NodeMsg input, ExecuteAction callback)
    {
        Node.Callback?.Invoke();

        return Task.CompletedTask;
    }

}
