using Mars.Nodes.Core;

namespace Mars.Host.Shared.Services;

public interface INodesReader
{
    Node? GetNode(string nodeId);

    IReadOnlyCollection<Node> Nodes(Func<Node, bool> expression);
}
