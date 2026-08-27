using Mars.Nodes.Core;
using Microsoft.AspNetCore.Components.Web;

namespace Mars.Nodes.Front.Shared.Editor.Models;

public class NodeComponentMouseEventArgs
{
    public MouseEventArgs MouseEvent;
    public Node Node;

    public NodeComponentMouseEventArgs(MouseEventArgs e, Node node)
    {
        MouseEvent = e;
        Node = node;
    }
}
