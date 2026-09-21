namespace Mars.Nodes.Core.Contracts.Nodes;

public record NodesDataResponse
{
    public required IReadOnlyCollection<Node> Nodes { get; init; }
    public required IDictionary<string, NodeStateInfoResponse> NodesState { get; init; }

    public required IReadOnlyCollection<InlineFunctionNodeSchemaResponse> InlineFunctionNodeSchemas { get; init; }

    /// <summary>Output specs of node types whose assemblies the front does not have; grouped by TypeId.</summary>
    public IReadOnlyDictionary<string, OutputValueSpec[]> OutputValueSpecs { get; init; } =
        new Dictionary<string, OutputValueSpec[]>();

    public IReadOnlyCollection<string> GlobalVariableNames { get; init; } = [];

    /// <summary>Global debug mode state; not persisted, off after a restart.</summary>
    public bool DebugMode { get; init; }
}

public record NodeStateInfoResponse
{
    public required string? Status { get; set; }
}
