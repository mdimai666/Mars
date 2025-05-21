using Mars.Nodes.Core;
using Mars.Nodes.Core.Dto;

namespace Mars.Nodes.Host.Mappings;

public static class NodeMappings
{

    public static NodesDataDto ToNodeDataDto(this IEnumerable<Node> nodes)
        => new()
        {
            Nodes = nodes.ToArray(),
            NodesState = nodes.Where(s => s.status != null).ToDictionary(node => node.Id, node => node.ToNodeStateInfoDto())
        };

    public static NodeStateInfoDto ToNodeStateInfoDto(this Node node)
        => new()
        {
            Status = node.status
        };
}
