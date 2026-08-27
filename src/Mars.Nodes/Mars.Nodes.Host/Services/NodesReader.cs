using Mars.Nodes.Abstractions.Services;
using Mars.Nodes.Core;
using Mars.Nodes.Abstractions;

namespace Mars.Nodes.Host.Services;

internal class NodesReader(INodeRuntime runtime) : INodesReader
{
    public Node? GetNode(string nodeId)
    {
        return runtime.BasicNodesDict.GetValueOrDefault(nodeId);
    }

    public IReadOnlyCollection<Node> Nodes(Func<Node, bool>? expression = null)
    {
        if (expression == null) return runtime.BasicNodesDict.Values.ToArray();

        return runtime.BasicNodesDict.Values.Where(expression).ToArray();
    }
}
