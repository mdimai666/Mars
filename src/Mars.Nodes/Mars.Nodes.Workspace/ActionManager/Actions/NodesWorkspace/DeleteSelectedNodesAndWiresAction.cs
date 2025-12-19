namespace Mars.Nodes.Workspace.ActionManager.Actions.NodesWorkspace;

[EditorActionCommand("DeleteSelectedNodes", "Delete")]
public class DeleteSelectedNodesAndWiresAction : BaseEditorHistoryAction
{
    DeleteNodesAndWiresAction _deleteNodesAndWiresAction;

    public DeleteSelectedNodesAndWiresAction(INodeEditorApi editor) : base(editor)
    {
        var selected = _editor.NodeWorkspace.SelectedNodes();

        _deleteNodesAndWiresAction = new(editor, selected);
    }

    public override bool CanExecute()
        => _editor.NodeWorkspace.SelectedNodes().Any() || _editor.NodeWorkspace.SelectedWires().Any();

    public override void Execute()
    {
        _deleteNodesAndWiresAction.Execute();
    }
    public override void Undo()
    {
        _deleteNodesAndWiresAction.Undo();
    }
}
