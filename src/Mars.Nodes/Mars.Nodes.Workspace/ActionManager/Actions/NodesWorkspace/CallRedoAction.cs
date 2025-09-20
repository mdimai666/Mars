using Mars.Nodes.EditorApi.Interfaces;

namespace Mars.Nodes.Workspace.ActionManager.Actions.NodesWorkspace;

[EditorActionCommand("Redo", "Ctrl+KeyY")]
public class CallRedoAction(INodeEditorApi editor) : IEditorAction
{
    public bool CanExecute() => true;

    public void Execute() => editor.ActionManager.Redo();

}
