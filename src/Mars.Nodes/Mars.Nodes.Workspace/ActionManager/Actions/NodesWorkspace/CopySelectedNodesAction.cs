using Mars.Nodes.Workspace.ActionManager.CopyBuffer;

namespace Mars.Nodes.Workspace.ActionManager.Actions.NodesWorkspace;

[EditorActionCommand("CopySelectedNodes", "Ctrl+KeyC")]
public class CopySelectedNodesAction : IEditorAction
{
    private readonly INodeEditorApi _editor;

    public CopySelectedNodesAction(INodeEditorApi editor)
    {
        _editor = editor;
    }

    public bool CanExecute() => _editor.NodeWorkspace.SelectedNodes().Any();

    public void Execute()
    {
        var nodes = _editor.NodeWorkspace.SelectedNodes();
        var buffer = new NodesCopyBufferItem(_editor, nodes);
        _editor.ActionManager.SetCopyBuffer(buffer);
    }

}
