using Mars.Nodes.Workspace.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Nodes.Workspace.ActionManager.Actions.NodesWorkspace;

[EditorActionCommand("OpenImportWindow")]
public class OpenImportWindowAction(INodeEditorApi _editor,
                                    IDialogService _dialogService,
                                    AppFront.Shared.Interfaces.IMessageService _messageService) : IEditorAction
{
    public bool CanExecute() => true;
    public void Execute() => _ = ExecuteAsync();
    public async ValueTask ExecuteAsync()
    {
        var dialog = await ImportNodesDialog.ShowDialog(_dialogService);
        var result = await dialog.Result;

        if (!result.Cancelled && result.Data is string json)
        {
            var action = new ImportNodesFromJsonAction(_editor, json);

            if (action.IsValid)
            {
                _editor.ActionManager.ExecuteAction(action);
                _ = _messageService.Success($"Imported: {action.NodesCount}");
            }
            else
            {
                _ = _messageService.Error("Invalid json");
            }
        }
    }

}
