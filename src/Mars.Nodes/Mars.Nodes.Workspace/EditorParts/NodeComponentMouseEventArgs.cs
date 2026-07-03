using Mars.Nodes.Core;
using Microsoft.AspNetCore.Components.Web;

namespace Mars.Nodes.Workspace.EditorParts;

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
