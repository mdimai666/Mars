using Mars.Nodes.Core;
using Mars.Nodes.Core.Utils;
using Mars.Nodes.Workspace.Components;

namespace Mars.Nodes.Workspace;

/// <summary>
/// Для построения NodeWire вычисляет координаты точек
/// </summary>
public class NodeWirePointResolver : INodeWirePointResolver
{
    public WirePoints GetPoints(Node node1, int node1outPort, Node node2, int node2InputPort)
    {
        float x1 = node1.X + NodeComponent.CalcBodyWidth(node1) + 15f;
        float y1 = node1.Outputs.Count <= 1 ? node1.Y + 23 : node1.Y + 16 + node1outPort * 16;

        float x2 = node2.X + 8;
        float y2 = node2.Inputs.Count <= 1 ? y2 = node2.Y + 23 : y2 = node2.Y + 16 + node2InputPort * 16;

        return new WirePoints(new(x1, y1), new(x2, y2));
    }
}
