namespace Mars.Nodes.Workspace.ActionManager.Actions.Globals;

[EditorActionCommand("DeployNodes", "Ctrl+KeyD")]
public class DeployNodesAction(INodeEditorApi editor) : IEditorAction
{
    public bool CanExecute() => true;

    public void Execute() => editor.DeployClick();

}
