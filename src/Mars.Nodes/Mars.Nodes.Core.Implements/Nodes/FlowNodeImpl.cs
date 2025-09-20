using Mars.Nodes.Core.Implements.Models;
using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes;

public class FlowNodeImpl : INodeImplement<FlowNode>, INodeImplement
{
    public FlowNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public VariablesContextDictionary Context => RED.FlowContext;

    public FlowNodeImpl(FlowNode node, IRED RED)
    {
        this.Node = node;
        this.RED = RED;
    }

    public Task Execute(NodeMsg _input, ExecuteAction callback, ExecutionParameters parameters)
    {
        throw new NotImplementedException();
    }
}
