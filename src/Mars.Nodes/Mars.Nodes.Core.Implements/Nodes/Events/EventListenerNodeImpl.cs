using Mars.Host.Shared.Managers;
using Mars.Nodes.Core.Nodes.Events;
using Mars.Nodes.Host.Shared;

namespace Mars.Nodes.Core.Implements.Nodes.Events;

public class EventListenerNodeImpl : INodeImplement<EventListenerNode>
{
    public EventListenerNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public EventListenerNodeImpl(EventListenerNode node, IRuntimeNodeScope rns)
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
