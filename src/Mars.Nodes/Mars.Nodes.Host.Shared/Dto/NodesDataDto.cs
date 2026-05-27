using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Host.Shared.Dto;

public class NodesDataDto
{
    public required IReadOnlyCollection<Node> Nodes { get; init; }
    public required IDictionary<string, NodeStateInfoDto> NodesState { get; init; }

    public required IReadOnlyCollection<InlineFunctionNodeSchema> InlineFunctionNodeSchemas { get; init; }
}
