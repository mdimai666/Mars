namespace Mars.Nodes.Core;

/// <summary>
/// Value a node puts into the message. <see cref="Path"/> is relative to the msg root
/// ("Payload", "user", "Payload.status"); "[]" means an element of an array.
/// <see cref="OutputPort"/> is the node output the value leaves through, <see cref="AllOutputPorts"/> —
/// every output (nodes can add outputs).
/// </summary>
public record OutputValueSpec(string Path, string VarType, int OutputPort = 0, string? Description = null)
{
    public const int DefaultOutputPort = 0;
    public const int AllOutputPorts = -1;
}
