namespace Mars.Nodes.Core;

public interface INodeExample<out TNode> where TNode : Node
{
    string Name { get; }
    string Description { get; }
    public IReadOnlyCollection<Node> Handle(IEditorState editorState);
}

public interface IEditorState
{
    public Node[] Nodes { get; }
}
