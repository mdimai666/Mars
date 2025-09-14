using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes;

public class CounterNodeImpl : INodeImplement<CounterNode>, INodeImplement
{
    public int Count { get; set; }

    public CounterNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public CounterNodeImpl(CounterNode node, IRED red)
    {
        Node = node;
        RED = red;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        Count += parameters.InputPort == 0 ? -1 : +1;
        input.Payload = Count;

        //RED.DebugMsg(DebugMessage.NodeMessage(Node.Id, $"input port = {parameters.InputPort}"));
        Node.status = $"count = {Count}";
        RED.Status(new NodeStatus(Node.status));

        callback(input);

        return Task.CompletedTask;
    }
}
