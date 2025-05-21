using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes;

public class SumNodeImpl : INodeImplement<SumNode>, INodeImplement
{
    public SumNodeImpl(SumNode node, IRED RED)
    {
        this.Node = node;
        this.RED = RED;
    }

    public SumNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public Task Execute(NodeMsg input, ExecuteAction callback, Action<Exception> Error)
    {
        int result = Node.a + Node.b;

        input.Payload = result;
        callback(input);

        return Task.CompletedTask;
    }
}
