using Mars.Core.Extensions;
using Mars.Nodes.Core;

namespace Mars.Nodes.Workspace.ActionManager.Actions.NodesWorkspace;

public class DeleteNodesAndWiresAction : BaseEditorHistoryAction
{
    private string _deletedNodesJson = default!;
    List<NodeConnect> _selectedConnectsToRemove = default!;
    List<NodeConnect> _dependConnectsToRemove = default!;

    public DeleteNodesAndWiresAction(INodeEditorApi editor, IEnumerable<Node> deleteNodes) : base(editor)
    {
        _deletedNodesJson = NodesToJson(deleteNodes);
        _dependConnectsToRemove = GetDependWires(deleteNodes);
        _selectedConnectsToRemove = _editor.NodeWorkspace.Wires.Where(s => s.Selected).Select(s => new NodeConnect(s.Node1, s.Node2)).ToList();
    }

    public override bool CanExecute() => true;

    public override void Execute()
    {
        var selected = NodesFromJson(_deletedNodesJson);
        _editor.DeleteNodesAndWires(selected, _selectedConnectsToRemove);
    }

    List<NodeConnect> GetDependWires(IEnumerable<Node> nodes)
    {
        var list = new List<NodeConnect>();
        var nodeIds = nodes.Select(s => s.Id).ToHashSet();

        var linkNodesIds = _editor.NodeWorkspace.Wires.Where(w => nodeIds.Contains(w.Node1.NodeId) || nodeIds.Contains(w.Node2.NodeId))
                        .SelectMany(s => (IEnumerable<string>)[s.Node1.NodeId, s.Node2.NodeId])
                        .Except(nodeIds)
                        .Distinct()
                        .ToList();

        foreach (var id in linkNodesIds)
        {
            var node = _editor.AllNodes[id];
            int outIndex = 0;
            foreach (var wireOuts in node.Wires)
            {
                var outsToRemove = wireOuts.Where(s => nodeIds.Contains(s.NodeId)).ToList();
                if (outsToRemove.Any())
                {
                    foreach (var wire in outsToRemove)
                        list.Add(new NodeConnect(new(id, outIndex), wire));
                }
                outIndex++;
            }
        }
        return list;
    }

    public override void Undo()
    {
        var deletedNodes = NodesFromJson(_deletedNodesJson);
        _editor.AddNodesAndWires(deletedNodes, [.. _selectedConnectsToRemove, .. _dependConnectsToRemove]);
    }
}
