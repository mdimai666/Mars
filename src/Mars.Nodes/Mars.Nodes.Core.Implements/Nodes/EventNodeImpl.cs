using Mars.Host.Shared.Managers;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Host.Shared;

namespace Mars.Nodes.Core.Implements.Nodes;

public class EventNodeImpl : INodeImplement<EventNode>
{
    public EventNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public EventNodeImpl(EventNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {

        ManagerEventPayload _event = input.Get<ManagerEventPayload>()!;

        input.Payload = _event;

        callback(input);

        return Task.CompletedTask;
    }

    public bool TestTopic(string value)
    {
        if (string.IsNullOrWhiteSpace(Node.Topics)) return false;
        if (Node.Topics.Trim() == "*") return true;

        var topics = Node.Topics.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var topic in topics)
        {
            if (IEventManager.TestTopic(topic, value))
            {
                return true;
            }
        }

        return false;
    }
}
