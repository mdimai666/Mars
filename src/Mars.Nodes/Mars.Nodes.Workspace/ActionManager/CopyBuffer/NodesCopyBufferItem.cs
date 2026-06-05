using System.Text.Json;
using Mars.Core.Extensions;
using Mars.Nodes.Core;
using Mars.Nodes.Workspace.ActionManager.Actions.NodesWorkspace;
using Microsoft.AspNetCore.Components.Web;

namespace Mars.Nodes.Workspace.ActionManager.CopyBuffer;

public class NodesCopyBufferItem : ICopyBufferItem
{
    private string _nodesJson;
    private readonly INodeEditorApi _editor;
    private readonly MouseEventArgs _onCopyMousePos;

    public NodesCopyBufferItem(INodeEditorApi editor, IEnumerable<Node> nodes)
    {
        _editor = editor;
        _nodesJson = NodesToJson(nodes);
        _onCopyMousePos = editor.NodeWorkspace.LastMouseWorkspaceState;
    }

    public bool CanPaste() => true;

    public void Paste()
    {
        var nodesCopy = CreateNodesCopies(_nodesJson, _editor.NodesJsonSerializerOptions);
        var flowId = _editor.ActiveFlow?.Id ?? throw new ArgumentNullException("ActiveFlow is null, ActiveFlow should be set");
        foreach (var node in nodesCopy)
        {
            node.Container = node.IsContainerless ? string.Empty : flowId;
        }

        var existNodesIds = _editor.AllNodes.Values.Select(s => s.Id).ToList();
        _editor.ActionManager.ExecuteAction(new CreateNodesAction(_editor, nodesCopy));

        var createdNodesIds = _editor.AllNodes.Values.Select(s => s.Id).Except(existNodesIds).ToList();
        var createdNodes = createdNodesIds.Select(id => _editor.AllNodes[id]);
        (float offsetX, float offsetY) = CalcOffset(createdNodes);
        _editor.NodeWorkspace.StartDragNodes(createdNodes, startMoveUnderCursor: true, offsetX, offsetY);

    }

    public static Node[] CreateNodesCopies(string nodesJson, JsonSerializerOptions jsonSerializerOptionsWithNodesConverter)
    {
        var nodes = NodesFromJson(nodesJson, jsonSerializerOptionsWithNodesConverter);
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

    private (float offsetX, float offsetY) CalcOffset(IEnumerable<Node> nodes)
    {
        var minX = nodes.Min(s => s.X);
        var minY = nodes.Min(s => s.Y);
        var offsetX = (float)_onCopyMousePos.OffsetX - minX;
        var offsetY = (float)_onCopyMousePos.OffsetY - minY;
        return (offsetX, offsetY);
    }

    public string NodesToJson(IEnumerable<Node> nodes)
        => NodesToJson(nodes, _editor.NodesJsonSerializerOptions);

    public static string NodesToJson(IEnumerable<Node> nodes, JsonSerializerOptions jsonSerializerOptionsWithNodesConverter)
        => JsonSerializer.Serialize(nodes, jsonSerializerOptionsWithNodesConverter);

    public Node[] NodesFromJson(string json)
        => NodesFromJson(json, _editor.NodesJsonSerializerOptions);

    public static Node[] NodesFromJson(string json, JsonSerializerOptions jsonSerializerOptionsWithNodesConverter)
        => JsonSerializer.Deserialize<Node[]>(json, jsonSerializerOptionsWithNodesConverter)!;
}
