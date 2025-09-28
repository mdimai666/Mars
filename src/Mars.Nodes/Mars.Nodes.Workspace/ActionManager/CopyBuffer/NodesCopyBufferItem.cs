using System.Text.Json;
using Mars.Core.Extensions;
using Mars.Nodes.Core;
using Mars.Nodes.EditorApi.Interfaces;
using Mars.Nodes.Workspace.ActionManager.Actions.NodesWorkspace;

namespace Mars.Nodes.Workspace.ActionManager.CopyBuffer;

public class NodesCopyBufferItem : ICopyBufferItem
{
    private string _nodesJson;
    private readonly INodeEditorApi _editor;

    public NodesCopyBufferItem(INodeEditorApi editor, IEnumerable<Node> nodes)
    {
        _nodesJson = NodesToJson(nodes);
        _editor = editor;
    }

    public bool CanPaste() => true;

    public void Paste()
    {
        var nodesCopy = CreateNodesCopies(_nodesJson);

        var existNodesIds = _editor.AllNodes.Values.Select(s => s.Id).ToList();
        _editor.ActionManager.ExecuteAction(new CreateNewNodesAction(_editor, nodesCopy));

        var createdNodesIds = _editor.AllNodes.Values.Select(s => s.Id).Except(existNodesIds).ToList();
        var createdNodes = createdNodesIds.Select(id => _editor.AllNodes[id]);
        _editor.NodeWorkspace.StartDragNodes(createdNodes);

    }

    public static Node[] CreateNodesCopies(string nodesJson)
    {
        var nodes = NodesFromJson(nodesJson);
        var idMap = nodes.Select(s => s.Id).ToDictionary(s => s, s => Guid.NewGuid().ToString());

        foreach (var node in nodes)
        {
            node.Id = idMap[node.Id];
            node.X += 10;
            node.Y += 10;

            for (var outPort = 0; outPort < node.Wires.Count; outPort++)
            {
                var wires = node.Wires[outPort];
                var newWires = new List<NodeWire>();
                for (var i = 0; i < wires.Count; i++)
                {
                    if (idMap.ContainsKey(wires[i].NodeId))
                    {
                        newWires.Add(new NodeWire(idMap[wires[i].NodeId], wires[i].PortIndex));
                    }
                }
                node.Wires[outPort] = newWires;
            }
        }
        return nodes;
    }

    public static string NodesToJson(IEnumerable<Node> nodes)
        => JsonSerializer.Serialize(nodes);

    public static Node[] NodesFromJson(string json)
        => JsonSerializer.Deserialize<Node[]>(json)!;
}
