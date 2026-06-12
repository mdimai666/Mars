using Mars.Nodes.Core;

namespace Mars.Nodes.Workspace.ActionManager.Actions.NodesWorkspace;

public class CreateWireAction : BaseEditorHistoryAction
{
    readonly NodeConnect[] _wiresConnect;

    public CreateWireAction(INodeEditorApi editor, NodeConnect[] wiresConnect) : base(editor)
    {
        _wiresConnect = wiresConnect;
    }

    public override bool CanExecute() => _editor.AllNodes.Any();

    public override void Execute()
    {
        _editor.AddNodesAndWires([], _wiresConnect);
    }

    public override void Undo()
    {
        _editor.DeleteNodesAndWires([], _wiresConnect);
    }
}
