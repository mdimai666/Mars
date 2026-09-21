namespace Mars.Nodes.Core;

/// <summary>
/// Truncated snapshot of the message a node sent out through one output port.
/// <see cref="Json"/> is the message as it looks in expressions: context keys at the root plus "Payload".
/// </summary>
public record NodeDebugSnapshot(string NodeId, int Port, DateTime CapturedAt, string Json);
