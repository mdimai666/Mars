using Mars.Nodes.Workspace.ActionManager.CopyBuffer;

namespace Mars.Nodes.Workspace.ActionManager.Actions.NodesWorkspace;

[EditorActionCommand("ImportNodesFromJson")]
public class ImportNodesFromJsonAction : BaseEditorHistoryAction, IDisposable
{
    public readonly int NodesCount;
    public readonly bool IsValid;

    private bool _dragEndPositionWritted;
    private string _pastedNodesJson;

    public ImportNodesFromJsonAction(INodeEditorApi editor, string json) : base(editor)
    {
        _editor.NodeWorkspace.OnDragNodesEnded += CatchDragNodesEnded;
        IsValid = IsValidate(json, out NodesCount);

        var nodesCopy = NodesCopyBufferItem.CreateNodesCopies(json, _editor.NodesJsonSerializerOptions);
        var flowId = _editor.ActiveFlow?.Id ?? throw new ArgumentNullException("ActiveFlow is null, ActiveFlow should be set");
        foreach (var node in nodesCopy)
        {
            node.Container = node.IsContainerless ? string.Empty : flowId;
        }
        _pastedNodesJson = NodesToJson(nodesCopy);
    }

    public override bool CanExecute() => IsValid;

    public override void Execute()
    {
        var nodesCopy = NodesFromJson(_pastedNodesJson);

        var existNodesIds = _editor.AllNodes.Values.Select(s => s.Id).ToList();
        _editor.ActionManager.ExecuteAction(new CreateNodesAction(_editor, nodesCopy), addToHistory: false);

        var createdNodesIds = _editor.AllNodes.Values.Select(s => s.Id).Except(existNodesIds).ToList();
        var createdNodes = createdNodesIds.Select(id => _editor.AllNodes[id]);
        if (!_dragEndPositionWritted)
            _editor.NodeWorkspace.StartDragNodes(createdNodes, startMoveUnderCursor: true);
    }

    public override void Undo()
    {
        var nodesCopy = NodesFromJson(_pastedNodesJson);
        var targetNodes = nodesCopy.Select(s => _editor.AllNodes[s.Id]).ToList();

        _editor.DeleteNodes(targetNodes);
    }

    void CatchDragNodesEnded(IEnumerable<string> nodeIds)
    {
        if (!_dragEndPositionWritted)
        {
            var nodes = NodesFromJson(_pastedNodesJson);
            foreach (var node in nodes)
            {
                node.X = _editor.AllNodes[node.Id].X;
                node.Y = _editor.AllNodes[node.Id].Y;
            }
            _pastedNodesJson = NodesToJson(nodes);
            _dragEndPositionWritted = true;
        }
    }

    bool IsValidate(string json, out int count)
    {
        try
        {
            var nodes = NodesCopyBufferItem.NodesFromJson(json, _editor.NodesJsonSerializerOptions);
            count = nodes.Length;
            return true;
        }
        catch
        {
            count = 0;
            return false;
        }
    }

    public void Dispose()
    {
        _editor.NodeWorkspace.OnDragNodesEnded -= CatchDragNodesEnded;
    }
}
