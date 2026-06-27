using Mars.Host.Shared.Managers;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Host.Shared;

namespace Mars.Nodes.Core.Implements.Nodes;

/// <summary>
/// <see cref="IActionManager"/>
/// <see cref="CommandNodesActionProvider"/>
/// </summary>
public class ActionCommandNodeImpl : INodeImplement<ActionCommandNode>
{
    public ActionCommandNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }

    Node INodeImplement.Node => Node;

    public ActionCommandNodeImpl(ActionCommandNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        callback(input);
        return Task.CompletedTask;
    }
}
