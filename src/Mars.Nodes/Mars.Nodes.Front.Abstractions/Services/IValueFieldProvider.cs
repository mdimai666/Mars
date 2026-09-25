using Mars.Nodes.Core;

namespace Mars.Nodes.Front.Abstractions.Services;

/// <summary>
/// Field a value input can suggest. <see cref="Path"/> is insertable into the field ("msg.Payload",
/// "GlobalContext.x"); <see cref="Source"/> names the node or host the field came from.
/// </summary>
public record ValueFieldInfo(string Path, string VarType, string? Source = null, string? Value = null);

/// <summary>Graph the edited node lives in; wires are on the nodes.</summary>
public record ValueFieldContext(IDictionary<string, Node> Nodes, Node EditedNode, string? FieldName = null);

public interface IValueFieldProvider
{
    IReadOnlyCollection<ValueFieldInfo> GetFields(ValueFieldContext context);
}

/// <summary>Provider of one path root ("msg", "GlobalContext", …); <see cref="Order"/> fixes the group order.</summary>
public interface IValueRootProvider
{
    int Order { get; }
    IEnumerable<ValueFieldInfo> GetFields(ValueFieldContext context);
}
