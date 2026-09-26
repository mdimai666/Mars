using Mars.Nodes.Front.Abstractions.Editor.Interfaces;
using Mars.Nodes.Workspace.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Nodes.Workspace.ActionManager.Actions.NodesWorkspace;

[EditorActionCommand("OpenExportWindow")]
public class OpenExportWindowAction(IDialogService _dialogService, INodeEditorApi _editor) : IEditorAction
{
    public bool CanExecute() => true;
    public void Execute() => ExportNodesDialog.ShowDialog(_dialogService, _editor);

}
