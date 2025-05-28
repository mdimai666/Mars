using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes;

public class InjectNodeImpl : INodeImplement<InjectNode>, INodeImplement
{
    public InjectNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public InjectNodeImpl(InjectNode node, IRED RED)
    {
        this.Node = node;
        this.RED = RED;
    }

    public Task Execute(NodeMsg _, ExecuteAction callback)
    {
        NodeMsg input = new NodeMsg();

        input.Payload = string.IsNullOrEmpty(Node.Payload) ? DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString() : Node.Payload;

        callback(input);

        return Task.CompletedTask;
    }
}
