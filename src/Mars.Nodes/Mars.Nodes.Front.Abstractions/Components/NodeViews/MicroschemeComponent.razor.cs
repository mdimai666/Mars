using Mars.Nodes.Core;
using Mars.Nodes.Front.Shared.Editor.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Mars.Nodes.Front.Shared.Components.NodeViews;

public partial class MicroschemeComponent
{
    [Parameter] public Node Node { get; set; } = default!;

    float X => Node.X + 10;
    float Y => Node.Y + 8;

    public float _bodyRectHeight => Node.BodyRectHeight;

    [Parameter] public EventCallback<NodeComponentMouseEventArgs> OnMouseDown { get; set; }
    [Parameter] public EventCallback<NodeComponentMouseEventArgs> OnMouseUp { get; set; }

    [Parameter] public EventCallback<NodeWirePointEventArgs> wireStartNew { get; set; }
    [Parameter] public EventCallback<NodeWirePointEventArgs> wireStartNewEnd { get; set; }

    [Parameter] public EventCallback<string> OnInject { get; set; }
    [Parameter] public EventCallback<NodeComponentMouseEventArgs> OnClick { get; set; }
    [Parameter] public EventCallback<NodeComponentMouseEventArgs> OnDblClick { get; set; }
    [Parameter] public EventCallback<NodeComponentMouseEventArgs> OnContextMenu { get; set; }

    void OnMouseDownMethod(MouseEventArgs e)
    {
        var a = new NodeComponentMouseEventArgs(e, Node);
        OnMouseDown.InvokeAsync(a);
    }
    void OnMouseUpMethod(MouseEventArgs e)
    {
        //OnInputWirePointUp(e);
        //OnOutputWirePointUp(e, 0);
        var a = new NodeComponentMouseEventArgs(e, Node);
        OnMouseUp.InvokeAsync(a);
    }

    void OnInjectClick(MouseEventArgs e)
    {
        OnInject.InvokeAsync(Node.Id);
    }

    // Simple events ============================
    void OnClickEvent(MouseEventArgs e)
    {
        var a = new NodeComponentMouseEventArgs(e, Node);
        OnClick.InvokeAsync(a);
    }
    void OnDblClickEvent(MouseEventArgs e)
    {
        var a = new NodeComponentMouseEventArgs(e, Node);
        OnDblClick.InvokeAsync(a);
    }
    void OnContextMenuEvent(MouseEventArgs e)
    {
        var a = new NodeComponentMouseEventArgs(e, Node);
        OnContextMenu.InvokeAsync(a);
    }
    // Wires ============================

    void OnInputWirePointDown(MouseEventArgs e)
    {
        wireStartNew.InvokeAsync(new NodeWirePointEventArgs(e, 0, true, Node));
    }

    void OnInputWirePointUp(MouseEventArgs e)
    {
        wireStartNewEnd.InvokeAsync(new NodeWirePointEventArgs(e, 0, true, Node));
    }

    void OnOutputWirePointDown(MouseEventArgs e, int index)
    {
        wireStartNew.InvokeAsync(new NodeWirePointEventArgs(e, index, false, Node));
    }

    void OnOutputWirePointUp(MouseEventArgs e, int index)
    {
        wireStartNewEnd.InvokeAsync(new NodeWirePointEventArgs(e, index, false, Node));
    }
}
