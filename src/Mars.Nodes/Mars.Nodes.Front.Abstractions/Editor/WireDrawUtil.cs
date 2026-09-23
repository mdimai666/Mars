using Mars.Nodes.Core;
using Mars.Nodes.Front.Abstractions.Editor.Interfaces;
using Mars.Nodes.Front.Abstractions.Editor.Models;

namespace Mars.Nodes.Front.Abstractions.Editor;

public static class WireDrawUtil
{
    public static Wire[] DrawWires(IReadOnlyDictionary<string, Node> nodes,
                                    INodeWirePointResolver nodeWirePointResolver)
    {
        if (nodeWirePointResolver == null)
            throw new ArgumentNullException(nameof(nodeWirePointResolver));

        var wires = new List<Wire>();

        foreach (var node in nodes.Values)
        {
            if (node.Wires == null) continue;

            for (int outputIndex = 0; outputIndex < node.Wires.Count; outputIndex++)
            {
                var outputWires = node.Wires[outputIndex];
                if (outputWires == null) continue;

                foreach (var wire in outputWires)
                {
                    if (!nodes.TryGetValue(wire.NodeId, out var targetNode))
                        continue;

                    var points = nodeWirePointResolver.GetPoints(node, outputIndex, targetNode, wire.PortIndex);

                    var newWire = new Wire
                    {
                        Id = $"{node.Id}#{outputIndex}->{wire}",
                        Node1 = new NodeWire(node.Id, outputIndex),
                        Node2 = wire,

                        X1 = points.Start.X,
                        Y1 = points.Start.Y,
                        X2 = points.End.X,
                        Y2 = points.End.Y
                    };

                    wires.Add(newWire);
                }
            }
        }

        return wires.ToArray();
    }

    public static void UpdateWiresPosition(Node node, Dictionary<(NodeWire, NodeWire), Wire> wiresDict, IReadOnlyDictionary<string, Node> nodes, INodeWirePointResolver nodeWirePointResolver)
    {
        for (int outputIndex = 0; outputIndex < node.Wires.Count; outputIndex++)
        {
            var outputWires = node.Wires[outputIndex];
            var startWire = new NodeWire(node.Id, outputIndex);

            foreach (var outWire in outputWires)
            {
                if (!nodes.TryGetValue(outWire.NodeId, out var targetNode))
                    continue;

                var points = nodeWirePointResolver.GetPoints(node, outputIndex, targetNode, outWire.PortIndex);

                if (wiresDict.TryGetValue((startWire, outWire), out var wireEl))
                {
                    wireEl.X1 = points.Start.X;
                    wireEl.Y1 = points.Start.Y;
                    wireEl.X2 = points.End.X;
                    wireEl.Y2 = points.End.Y;
                }
            }
        }

    }
}
