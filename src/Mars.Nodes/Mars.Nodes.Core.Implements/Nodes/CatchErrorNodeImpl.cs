using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes;

public class CatchErrorNodeImpl : INodeImplement<CatchErrorNode>, INodeImplement
{
    public CatchErrorNode Node { get; }
    Node INodeImplement<Node>.Node => Node;
    public IRED RED { get; set; }

    public CatchErrorNodeImpl(CatchErrorNode node, IRED red)
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
