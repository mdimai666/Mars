using Mars.Nodes.Core;
using Mars.Nodes.Workspace.EditorParts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Mars.Nodes.Workspace.Components.NodeViews;

public partial class MicroschemeComponent
{
    [Parameter] public Node node { get; set; } = default!;

    [Parameter] public float x { get; set; }
    [Parameter] public float y { get; set; }

    float X => node.X + 10;
    float Y => node.Y + 8;

    public float bodyRectHeight => node.Outputs.Count < 2 ? 30 : node.Outputs.Count * 16f;

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
        var a = new NodeComponentMouseEventArgs(e, node);
        OnMouseDown.InvokeAsync(a);
    }
    void OnMouseUpMethod(MouseEventArgs e)
    {
        //OnInputWirePointUp(e);
        //OnOutputWirePointUp(e, 0);
        var a = new NodeComponentMouseEventArgs(e, node);
        OnMouseUp.InvokeAsync(a);
    }

    void OnInjectClick(MouseEventArgs e)
    {
        OnInject.InvokeAsync(this.node.Id);
    }

    // Simple events ============================
    void OnClickEvent(MouseEventArgs e)
    {
        var a = new NodeComponentMouseEventArgs(e, node);
        OnClick.InvokeAsync(a);
    }
    void OnDblClickEvent(MouseEventArgs e)
    {
        var a = new NodeComponentMouseEventArgs(e, node);
        OnDblClick.InvokeAsync(a);
    }
    void OnContextMenuEvent(MouseEventArgs e)
    {
        var a = new NodeComponentMouseEventArgs(e, node);
        OnContextMenu.InvokeAsync(a);
    }
    // Wires ============================

    void OnInputWirePointDown(MouseEventArgs e)
    {
        wireStartNew.InvokeAsync(new NodeWirePointEventArgs(e, 0, true, node));
    }

    void OnInputWirePointUp(MouseEventArgs e)
    {
        wireStartNewEnd.InvokeAsync(new NodeWirePointEventArgs(e, 0, true, node));
    }

    void OnOutputWirePointDown(MouseEventArgs e, int index)
    {
        wireStartNew.InvokeAsync(new NodeWirePointEventArgs(e, index, false, node));
    }

    void OnOutputWirePointUp(MouseEventArgs e, int index)
    {
        wireStartNewEnd.InvokeAsync(new NodeWirePointEventArgs(e, index, false, node));
    }
}
