using Mars.Host.Shared.Managers;
using Mars.Nodes.Core.Nodes.Connections;
using Mars.Nodes.Host.Shared;

namespace Mars.Nodes.Core.Implements.Nodes.Connections;

public class ExecXActionNodeImpl : INodeImplement<ExecXActionNode>
{
    private readonly IActionManager _actionManager;

    public ExecXActionNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public ExecXActionNodeImpl(ExecXActionNode node, IRuntimeNodeScope rns, IActionManager actionManager)
    {
        Node = node;
        RNS = rns;
        _actionManager = actionManager;
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        var result = await _actionManager.Inject(Node.CommandId, [], parameters.CancellationToken);

        input.Payload = result;

        callback(input);
    }

}
