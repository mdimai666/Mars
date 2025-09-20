using Mars.Nodes.Core;
using Mars.Nodes.EditorApi.Interfaces;

namespace Mars.Nodes.Workspace.ActionManager.Actions.NodesWorkspace;

public class CreateWireAction : BaseEditorHistoryAction
{
    readonly NodeWireConnect[] _wiresConnect;

    public CreateWireAction(INodeEditorApi editor, NodeWireConnect[] wiresConnect) : base(editor)
    {
        _wiresConnect = wiresConnect;
    }

    public override bool CanExecute() => _editor.AllNodes.Any();

    public override void Execute()
    {
        foreach (var connect in _wiresConnect)
        {
            _editor.AllNodes[connect.Node1.NodeId].Wires.ElementAt(connect.Node1.PortIndex).Add(connect.Node2);
        }
        _editor.NodeWorkspace.RedrawWires();
    }

    public override void Undo()
    {
        foreach (var connect in _wiresConnect)
        {
            _editor.AllNodes[connect.Node1.NodeId].Wires.ElementAt(connect.Node1.PortIndex).Remove(connect.Node2);
        }
        _editor.NodeWorkspace.RedrawWires();
    }
}

public record NodeWireConnect(NodeWire Node1, NodeWire Node2);
