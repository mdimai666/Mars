using Mars.Nodes.Core;

namespace Mars.Nodes.Front.Shared.Contracts.Nodes;

public class NodesDataResponse
{
    public required IReadOnlyCollection<Node> Nodes { get; init; }
    public required IDictionary<string, NodeStateInfoResponse> NodesState { get; init; }
}

public class NodeStateInfoResponse
{
    public required string? Status { get; set; }
}
