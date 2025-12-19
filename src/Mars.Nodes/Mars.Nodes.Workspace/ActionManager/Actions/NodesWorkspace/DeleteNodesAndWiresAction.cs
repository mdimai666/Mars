using System.Text.Json;
using Mars.Core.Extensions;
using Mars.Nodes.Core;

namespace Mars.Nodes.Workspace.ActionManager.Actions.NodesWorkspace;

public class DeleteNodesAndWiresAction : BaseEditorHistoryAction
{
    private string _deletedNodesJson = default!;
    private string _deletedWiresJson = default!;
    IReadOnlyDictionary<string, List<(int outIndex, NodeWire[] wires)>> _dependWires = default!;

    public DeleteNodesAndWiresAction(INodeEditorApi editor, IEnumerable<Node> deleteNodes) : base(editor)
    {
        _deletedNodesJson = NodesToJson(deleteNodes);
        _dependWires = GetDependWires(deleteNodes);
        var selWires = _editor.NodeWorkspace.Wires.Where(s => s.Selected).ToList();
        _deletedWiresJson = JsonSerializer.Serialize(selWires);
    }

    public override bool CanExecute() => true;

    public override void Execute()
    {
        var selected = NodesFromJson(_deletedNodesJson);
        var selWires = JsonSerializer.Deserialize<Wire[]>(_deletedWiresJson)!;

        _editor.DeleteNodes(selected);
        RemoveWires(selWires);
        _editor.NodeWorkspace.RedrawWires();
        Tools.SetTimeout(_editor.NodeWorkspace.RedrawWires, 1);//хак, не сразу обновляется
    }

    IReadOnlyDictionary<string, List<(int outIndex, NodeWire[] wires)>> GetDependWires(IEnumerable<Node> nodes)
    {
        var dict = new Dictionary<string, List<(int outIndex, NodeWire[])>>();
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
            var outs = new List<(int outIndex, NodeWire[])>();
            foreach (var wireOuts in node.Wires)
            {
                var outsToRemove = wireOuts.Where(s => nodeIds.Contains(s.NodeId)).ToList();
                if (outsToRemove.Any())
                {
                    outs.Add((outIndex, outsToRemove.ToArray()));
                }
                outIndex++;
            }
            if (outs.Any())
            {
                dict.Add(id, outs);
            }
        }
        return dict;
    }

    void RemoveWires(IEnumerable<Wire> selWires)
    {
        foreach (var w in selWires)
        {
            foreach (var item in _editor.AllNodes[w.Node1].Wires)
            {
                item.Remove(w.Node2);
            }
        }
    }

    void RestoreWires(IEnumerable<Wire> selWires)
    {
        foreach (var w in selWires)
        {
            foreach (var item in _editor.AllNodes[w.Node1].Wires)
            {
                item.Add(w.Node2);
            }
        }
    }

    public override void Undo()
    {
        var deletedNodes = NodesFromJson(_deletedNodesJson);

        foreach (var (nodeId, wires) in _dependWires)
        {
            var node = _editor.AllNodes[nodeId];
            foreach (var w in wires)
            {
                node.Wires.ElementAt(w.outIndex).AddRange(w.wires);
            }
        }
        _editor.AddNodes(deletedNodes);
        var deletedWires = JsonSerializer.Deserialize<Wire[]>(_deletedWiresJson)!;
        RestoreWires(deletedWires);
        _editor.NodeWorkspace.RedrawWires();
        Tools.SetTimeout(_editor.NodeWorkspace.RedrawWires, 1);//хак, не сразу обновляется
    }
}
