namespace Mars.Nodes.Core.Dto;

public class NodesDataDto
{
    public required IReadOnlyCollection<Node> Nodes { get; init; }
    public required IDictionary<string, NodeStateInfoDto> NodesState { get; init; }
}
