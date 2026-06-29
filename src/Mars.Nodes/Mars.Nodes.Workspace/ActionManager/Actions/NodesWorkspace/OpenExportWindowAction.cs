using Mars.Nodes.Workspace.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Nodes.Workspace.ActionManager.Actions.NodesWorkspace;

[EditorActionCommand("OpenExportWindow")]
public class OpenExportWindowAction(IDialogService _dialogService) : IEditorAction
{
    public bool CanExecute() => true;
    public void Execute() => ExportNodesDialog.ShowDialog(_dialogService);

}
