using Mars.Nodes.Core.Nodes.Common;
using Mars.Nodes.Host.Shared;

namespace Mars.Nodes.Core.Implements.Nodes.Common;

public class InjectNodeImpl : INodeImplement<InjectNode>
{
    public InjectNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public InjectNodeImpl(InjectNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {

        input.Payload = string.IsNullOrEmpty(Node.Payload) ? DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString() : Node.Payload;

        callback(input);

        return Task.CompletedTask;
    }
}
