namespace Mars.Nodes.Core;

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
