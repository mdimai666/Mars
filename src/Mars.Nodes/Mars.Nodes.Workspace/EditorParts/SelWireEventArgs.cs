using Mars.Nodes.Core;
using Microsoft.AspNetCore.Components.Web;

namespace Mars.Nodes.Workspace.EditorParts;

public class SelWireEventArgs
{
    public MouseEventArgs MouseEvent;
    public NodeWire Node1;
    public NodeWire Node2;
    public SelWireEventArgs(MouseEventArgs e, NodeWire node1, NodeWire node2)
    {
        MouseEvent = e;
        Node1 = node1;
        Node2 = node2;
    }
}
