namespace Mars.Nodes.Contracts.Nodes;

/// <summary>
/// Truncated snapshot of the message a node sent out through one output port.
/// <see cref="Json"/> is the message as it looks in expressions: context keys at the root plus "Payload".
/// <see cref="Values"/> is the same data flattened to "path → value" (array items get concrete indexes),
/// produced on the server so clients do not re-parse <see cref="Json"/>.
/// </summary>
public record NodeDebugSnapshot(string NodeId, int Port, DateTime CapturedAt, string Json,
    IReadOnlyDictionary<string, string>? Values = null)
{
    /// <summary>Единый формат ключа «нода + выходной порт» для стора и клиентских кэшей.</summary>
    public static string Key(string nodeId, int port) => $"{nodeId}|{port}";
}

/// <summary>
/// Full object stored by a <c>DebugNode</c> with <c>StoreFullObject</c> on: last one per node,
/// hard 2 MB cap. Over the cap <see cref="Json"/> is cut mid-structure (invalid JSON) and
/// <see cref="Truncated"/> is set — the viewer falls back to raw text.
/// </summary>
public record NodeDebugFullSnapshot(string NodeId, DateTime CapturedAt, string Json, bool Truncated);
