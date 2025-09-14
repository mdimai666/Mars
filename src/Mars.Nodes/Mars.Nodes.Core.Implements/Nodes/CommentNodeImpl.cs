using Mars.Nodes.Core.Implements.Models;
using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes;

public class CommentNodeImpl : INodeImplement<CommentNode>, INodeImplement
{
    public CommentNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public VariablesContextDictionary Context => RED.FlowContext;

    public CommentNodeImpl(CommentNode node, IRED red)
    {
        Node = node;
        RED = red;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        throw new NotImplementedException();
    }
}
