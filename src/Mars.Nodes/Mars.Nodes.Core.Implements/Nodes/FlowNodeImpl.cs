using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Host.Shared;
using Mars.Nodes.Host.Shared.Models;

namespace Mars.Nodes.Core.Implements.Nodes;

public class FlowNodeImpl : INodeImplement<FlowNode>, IFlowNodeImpl
{
    public FlowNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    public VariablesContextDictionary Context => RNS.FlowContext!;

    Node INodeImplement.Node => Node;

    public FlowNodeImpl(FlowNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public Task Execute(NodeMsg _input, ExecuteAction callback, ExecutionParameters parameters)
    {
        throw new NotSupportedException();
    }
}
