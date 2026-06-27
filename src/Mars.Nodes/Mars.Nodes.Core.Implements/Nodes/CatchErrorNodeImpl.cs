using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Host.Shared;

namespace Mars.Nodes.Core.Implements.Nodes;

public class CatchErrorNodeImpl : INodeImplement<CatchErrorNode>
{
    public CatchErrorNode Node { get; }
    Node INodeImplement.Node => Node;
    public IRuntimeNodeScope RNS { get; set; }

    public CatchErrorNodeImpl(CatchErrorNode node, IRuntimeNodeScope rns)
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
