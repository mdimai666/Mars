using Mars.Nodes.Core;
using Mars.Nodes.Front.Shared.Contracts.Nodes;

namespace Mars.Nodes.Host.Mappings.Nodes;

public static class NodeResponseMappings
{
    public static NodesDataResponse ToResponse(this IEnumerable<Node> nodes)
        => new()
        {
            Nodes = nodes.ToArray(),
            NodesState = nodes.Where(s => s.status != null).ToDictionary(node => node.Id, node => node.ToResponse())
        };

    public static NodeStateInfoResponse ToResponse(this Node node)
        => new()
        {
            Status = node.status
        };

}
