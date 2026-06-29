using Mars.Nodes.Workspace.ActionManager.CopyBuffer;

namespace Mars.Nodes.Workspace.ActionManager.Actions.NodesWorkspace;

[EditorActionCommand("CopySelectedNodesAsJson")]
public class CopySelectedNodesAsJsonAction : IEditorAction
{
    private readonly INodeEditorApi _editor;

    public CopySelectedNodesAsJsonAction(INodeEditorApi editor)
    {
        _editor = editor;
    }

    public bool CanExecute() => _editor.NodeWorkspace.SelectedNodes().Any();

    public void Execute()
    {
        var nodes = _editor.NodeWorkspace.SelectedNodes();
        var copies = NodesCopyBufferItem.NodesToJson(nodes, _editor.NodesJsonSerializerOptionsFormatted);

        _editor.ActionManager.CopyToClipboard(copies);
    }

}
