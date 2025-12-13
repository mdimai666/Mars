using Mars.Nodes.EditorApi.Interfaces;
using Mars.Nodes.Workspace.ActionManager.CopyBuffer;

namespace Mars.Nodes.Workspace.ActionManager.Actions.NodesWorkspace;

[EditorActionCommand("DuplicateSelectedNodes", "Ctrl+KeyD")]
public class DuplicateSelectedNodesAction : BaseEditorHistoryAction
{
    private readonly string _selectNodesJson;

    public DuplicateSelectedNodesAction(INodeEditorApi editor) : base(editor)
    {
        var selectNodes = _editor.NodeWorkspace.SelectedNodes();
        var selectNodesJson = NodesToJson(selectNodes);
        var nodesCopy = NodesCopyBufferItem.CreateNodesCopies(selectNodesJson, _editor.NodesJsonSerializerOptions);
        _selectNodesJson = NodesToJson(nodesCopy);
    }

    public override bool CanExecute() => _editor.NodeWorkspace.SelectedNodes().Any();

    public override void Execute()
    {
        var nodes = NodesFromJson(_selectNodesJson);
        _editor.ActionManager.ExecuteAction(new CreateNodesAction(_editor, nodes));

        var createdNodes = nodes.Select(node => _editor.AllNodes[node.Id]);
        _editor.NodeWorkspace.StartDragNodes(createdNodes);
    }

    public override void Undo()
    {
        var nodesCopy = NodesFromJson(_selectNodesJson);
        var targetNodes = nodesCopy.Select(s => _editor.AllNodes[s.Id]).ToList();

        _editor.DeleteNodes(targetNodes);
        _editor.NodeWorkspace.RedrawWires();
    }
}
