namespace Mars.Nodes.Core.Utils;

public interface INodeWirePointResolver
{
    public WirePoints GetPoints(Node node1, int node1OutPort, Node node2, int node2InputPort);
}
