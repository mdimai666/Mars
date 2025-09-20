using Mars.Host.Shared.Managers;
using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes;

/// <summary>
/// <see cref="IActionManager"/>
/// <see cref="CommandNodesActionProvider"/>
/// </summary>
public class ActionCommandNodeImpl : INodeImplement<ActionCommandNode>, INodeImplement
{
    public ActionCommandNode Node { get; }
    public IRED RED { get; set; }

    Node INodeImplement<Node>.Node => Node;

    public ActionCommandNodeImpl(ActionCommandNode node, IRED red)
    {
        Node = node;
        RED = red;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        callback(input);
        return Task.CompletedTask;
    }
}
