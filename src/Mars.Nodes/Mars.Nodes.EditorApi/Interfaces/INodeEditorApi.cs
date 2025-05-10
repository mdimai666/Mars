using Mars.Nodes.Core;

namespace Mars.Nodes.EditorApi.Interfaces;

public interface INodeEditorApi
{
    List<Type> RegisteredNodes { get; }
    List<Node> Palette { get; }
    List<Node> Nodes { get; }
}
