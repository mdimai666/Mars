namespace Mars.Nodes.Core.Contracts.Nodes;

public record NodesDataResponse
{
    public required IReadOnlyCollection<Node> Nodes { get; init; }
    public required IDictionary<string, NodeStateInfoResponse> NodesState { get; init; }

    public required IReadOnlyCollection<InlineFunctionNodeSchemaResponse> InlineFunctionNodeSchemas { get; init; }
}

public record NodeStateInfoResponse
{
    public required string? Status { get; set; }
}
