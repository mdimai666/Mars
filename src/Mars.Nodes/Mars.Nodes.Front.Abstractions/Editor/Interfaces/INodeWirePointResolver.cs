using Mars.Nodes.Core;
using Mars.Nodes.Front.Abstractions.Editor.Models;

namespace Mars.Nodes.Front.Abstractions.Editor.Interfaces;

public interface INodeWirePointResolver
{
    public WirePoints GetPoints(Node node1, int node1OutPort, Node node2, int node2InputPort);
}
