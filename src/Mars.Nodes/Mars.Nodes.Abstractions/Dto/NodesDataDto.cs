using Mars.Nodes.Core.Nodes.Functions;
using Mars.Nodes.Core;

namespace Mars.Nodes.Abstractions.Dto;

public class NodesDataDto
{
    public required IReadOnlyCollection<Node> Nodes { get; init; }
    public required IDictionary<string, NodeStateInfoDto> NodesState { get; init; }

    public required IReadOnlyCollection<InlineFunctionNodeSchema> InlineFunctionNodeSchemas { get; init; }
}
