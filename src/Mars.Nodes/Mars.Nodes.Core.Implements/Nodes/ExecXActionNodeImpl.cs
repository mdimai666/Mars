using Mars.Host.Shared.Managers;
using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes;

public class ExecXActionNodeImpl : INodeImplement<ExecXActionNode>, INodeImplement
{
    private readonly IActionManager _actionManager;

    public ExecXActionNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public ExecXActionNodeImpl(ExecXActionNode node, IRED red, IActionManager actionManager)
    {
        Node = node;
        RED = red;
        _actionManager = actionManager;
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        var result = await _actionManager.Inject(Node.CommandId, [], parameters.CancellationToken);

        input.Payload = result;

        callback(input);
    }

}
