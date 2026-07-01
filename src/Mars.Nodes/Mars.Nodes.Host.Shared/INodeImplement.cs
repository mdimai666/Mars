using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes.Common;
using Mars.Nodes.Host.Shared.Models;

namespace Mars.Nodes.Host.Shared;

/// <summary>
/// use <see cref="ISelfFinalizingNode"/> for control manual ending;
/// </summary>
public interface INodeImplement
{
    public Node Node { get; }
    public string Id => Node.Id;
    public IRuntimeNodeScope RNS { get; set; }

    //public INodeImplement<TNode> Create(TNode node);
    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters);
}

public interface INodeImplement<TNode> : INodeImplement where TNode : Node
{
    new TNode Node { get; }

}

public interface IFlowNodeImpl : INodeImplement<FlowNode>
{
    new FlowNode Node { get; }
    VariablesContextDictionary Context { get; }
}

public interface ISelfFinalizingNode
{
    //public IRuntimeNodeScope RNS { get; set; }
    //public void Done(ExecutionParameters parameters) => RNS.Done(parameters);
}

//public interface INodeImplementMultipleResult
//{

//}

public interface INodeLifecycleOnAssigned
{
    Task OnNodeAssigned(CancellationToken cancellationToken);
}

public interface INodeLifecycleOnDelete
{
    Task OnNodeDelete();
}

public delegate void ExecuteAction(NodeMsg msg, int output = 0);
