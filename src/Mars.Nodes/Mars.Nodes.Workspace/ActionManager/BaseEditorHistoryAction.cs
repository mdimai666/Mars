using Mars.Nodes.Core;
using Mars.Nodes.Workspace.ActionManager.CopyBuffer;

namespace Mars.Nodes.Workspace.ActionManager;

public abstract class BaseEditorHistoryAction : IEditorHistoryAction
{
    public Guid Guid { get; } = Guid.NewGuid();

    protected readonly INodeEditorApi _editor;

    protected BaseEditorHistoryAction(INodeEditorApi editor)
    {
        _editor = editor;
    }

    public string NodesToJson(IEnumerable<Node> nodes)
        => NodesCopyBufferItem.NodesToJson(nodes, _editor.NodesJsonSerializerOptions);

    public Node[] NodesFromJson(string json)
        => NodesCopyBufferItem.NodesFromJson(json, _editor.NodesJsonSerializerOptions);

    public abstract void Execute();
    public abstract void Undo();
    public abstract bool CanExecute();
}
