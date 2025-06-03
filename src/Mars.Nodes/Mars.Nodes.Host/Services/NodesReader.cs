using Mars.Host.Shared.Services;
using Mars.Nodes.Core;

namespace Mars.Nodes.Host.Services;

internal class NodesReader(RED _RED) : INodesReader
{
    public Node? GetNode(string nodeId)
    {
        return _RED.BasicNodesDict.GetValueOrDefault(nodeId);
    }

    public IReadOnlyCollection<Node> Nodes(Func<Node, bool>? expression = null)
    {
        if (expression == null) return _RED.BasicNodesDict.Values.ToArray();

        return _RED.BasicNodesDict.Values.Where(expression).ToArray();
    }
}
