using Mars.Nodes.Core;

namespace Mars.Nodes.Contracts.Nodes;

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

/// <summary>
/// Pull-ответ со снимками DebugMode: серверное время и флаг режима — чтобы клиент считал возраст
/// снимков и синхронизировал тумблер, не полагаясь на свои часы.
/// </summary>
public record NodeDebugSnapshotsResponse
{
    public DateTime ServerTimeUtc { get; init; }
    public bool DebugMode { get; init; }
    public IReadOnlyDictionary<string, NodeDebugSnapshot[]> Snapshots { get; init; } =
        new Dictionary<string, NodeDebugSnapshot[]>();
}

/// <summary>
/// Ответ про полный объект DebugNode: метаданные всегда, <see cref="Json"/> — только при
/// <c>includeJson</c> (форма тянет тяжёлое тело лишь при открытии модалки).
/// </summary>
public record NodeDebugFullResponse
{
    public DateTime ServerTimeUtc { get; init; }
    public DateTime? CapturedAt { get; init; }
    public int Size { get; init; }
    public bool Truncated { get; init; }
    public string? Json { get; init; }
}
