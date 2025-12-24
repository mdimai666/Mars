using Mars.Nodes.Core;

namespace Mars.Nodes.Host.Shared.Dto;

public class NodesDataDto
{
    public required IReadOnlyCollection<Node> Nodes { get; init; }
    public required IDictionary<string, NodeStateInfoDto> NodesState { get; init; }
}
