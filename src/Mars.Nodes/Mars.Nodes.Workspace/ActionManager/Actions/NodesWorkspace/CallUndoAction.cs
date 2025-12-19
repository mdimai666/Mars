namespace Mars.Nodes.Workspace.ActionManager.Actions.NodesWorkspace;

[EditorActionCommand("Undo", "Ctrl+KeyZ")]
public class CallUndoAction(INodeEditorApi editor) : IEditorAction
{
    public bool CanExecute() => true;

    public void Execute() => editor.ActionManager.Undo();

}
