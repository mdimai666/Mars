using Microsoft.AspNetCore.Components.Web;
using Mars.Nodes.Core;

namespace Mars.Nodes.Front.Abstractions.Editor.Models;

public class NodeWirePointEventArgs
{
    public MouseEventArgs MouseEvent;
    public int PinIndex;
    public bool IsInput;
    public Node Node;

    public NodeWirePointEventArgs(MouseEventArgs e, int pinIndex, bool isInput, Node node)
    {
        MouseEvent = e;
        PinIndex = pinIndex;
        IsInput = isInput;
        Node = node;
    }
}
