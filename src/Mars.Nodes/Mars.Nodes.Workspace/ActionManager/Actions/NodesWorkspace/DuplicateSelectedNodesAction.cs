using Mars.Nodes.Workspace.ActionManager.CopyBuffer;

namespace Mars.Nodes.Workspace.ActionManager.Actions.NodesWorkspace;

[EditorActionCommand("DuplicateSelectedNodes", "Ctrl+Shift+KeyD")]
public class DuplicateSelectedNodesAction : BaseEditorHistoryAction, IDisposable
{
    private string _selectNodesJson;
    private bool _dragEndPositionWritted;

    public DuplicateSelectedNodesAction(INodeEditorApi editor) : base(editor)
    {
        var selectNodes = _editor.NodeWorkspace.SelectedNodes();
        var selectNodesJson = NodesToJson(selectNodes);
        var nodesCopy = NodesCopyBufferItem.CreateNodesCopies(selectNodesJson, _editor.NodesJsonSerializerOptions);
        _selectNodesJson = NodesToJson(nodesCopy);
        _editor.NodeWorkspace.OnDragNodesEnded += CatchDragNodesEnded;
    }

    public override bool CanExecute() => _editor.NodeWorkspace.SelectedNodes().Any();

    public override void Execute()
    {
        var nodes = NodesFromJson(_selectNodesJson);
        _editor.ActionManager.ExecuteAction(new CreateNodesAction(_editor, nodes));

        var createdNodes = nodes.Select(node => _editor.AllNodes[node.Id]);

        if (!_dragEndPositionWritted)
            _editor.NodeWorkspace.StartDragNodes(createdNodes);
    }

    public override void Undo()
    {
        var nodesCopy = NodesFromJson(_selectNodesJson);
        var targetNodes = nodesCopy.Select(s => _editor.AllNodes[s.Id]).ToList();

        _editor.DeleteNodes(targetNodes);
    }

    void CatchDragNodesEnded(IEnumerable<string> nodeIds)
    {
        if (!_dragEndPositionWritted)
        {
            var nodes = NodesFromJson(_selectNodesJson);
            foreach (var node in nodes)
            {
                node.X = _editor.AllNodes[node.Id].X;
                node.Y = _editor.AllNodes[node.Id].Y;
            }
            _selectNodesJson = NodesToJson(nodes);
            _dragEndPositionWritted = true;
        }
    }

    public void Dispose()
    {
        _editor.NodeWorkspace.OnDragNodesEnded -= CatchDragNodesEnded;
    }
}
