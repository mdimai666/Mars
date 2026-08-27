using Mars.Nodes.Core;

namespace Mars.Nodes.Abstractions.Services;

public interface INodesReader
{
    Node? GetNode(string nodeId);

    IReadOnlyCollection<Node> Nodes(Func<Node, bool> expression);
}
