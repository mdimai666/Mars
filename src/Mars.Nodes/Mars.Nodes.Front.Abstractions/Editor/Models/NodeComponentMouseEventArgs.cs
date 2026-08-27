using Microsoft.AspNetCore.Components.Web;
using Mars.Nodes.Core;

namespace Mars.Nodes.Front.Abstractions.Editor.Models;

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
