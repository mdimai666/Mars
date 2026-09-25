namespace Mars.Nodes.Core;

/// <summary>
/// Declares a message slot a node produces, by value type; the type is expanded into paths by
/// <see cref="OutputValueSpecExpander"/>. Several attributes per node are allowed.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class NodeOutputValueSpecAttribute : Attribute
{
    public NodeOutputValueSpecAttribute(Type valueType)
    {
        ValueType = valueType;
    }

    public Type ValueType { get; }

    public string Name { get; init; } = "Payload";

    /// <summary>Output the value leaves through; <see cref="OutputValueSpec.AllOutputPorts"/> — every output.</summary>
    public int OutputPort { get; init; } = OutputValueSpec.DefaultOutputPort;

    public string? Description { get; init; }
}
