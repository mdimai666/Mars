using Mars.Nodes.EditorApi.Interfaces;

namespace Mars.Nodes.Workspace.ActionManager.Actions.NodesWorkspace;

[EditorActionCommand("SelectAllNodes", "Ctrl+KeyA")]
public class SelectAllNodesAction : BaseEditorHistoryAction
{
    private HashSet<string> _selNodesIds = [];

    public SelectAllNodesAction(INodeEditorApi editor) : base(editor)
    {
        _selNodesIds = _editor.NodeWorkspace.SelectedNodes().Select(s => s.Id).ToHashSet();
    }

    public override bool CanExecute() => _editor.AllNodes.Any();

    public override void Execute()
    {
        foreach (var node in _editor.NodeWorkspace.FlowNodes.Values)
            node.selected = true;
        _editor.NodeWorkspace.CallStateHasChanged();
    }

    public override void Undo()
    {
        foreach (var node in _editor.NodeWorkspace.FlowNodes.Values)
            node.selected = _selNodesIds.Contains(node.Id);
        _editor.NodeWorkspace.CallStateHasChanged();
    }
}
