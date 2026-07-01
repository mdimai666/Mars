using Mars.Nodes.Core.Nodes.Common;
using Mars.Nodes.Host.Shared;

namespace Mars.Nodes.Core.Implements.Nodes.Common;

public class LinkInNodeImpl : INodeImplement<LinkInNode>
{
    public LinkInNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public LinkInNodeImpl(LinkInNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        throw new NotSupportedException();
    }
}

public class LinkOutNodeImpl : INodeImplement<LinkOutNode>
{
    public LinkOutNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public LinkOutNodeImpl(LinkOutNode node, IRuntimeNodeScope rns)
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
