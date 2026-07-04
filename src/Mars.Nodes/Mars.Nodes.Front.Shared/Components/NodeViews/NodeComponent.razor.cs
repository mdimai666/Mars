using Mars.Core.Extensions;
using Mars.Nodes.Core;
using Mars.Nodes.Front.Shared.Editor.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Mars.Nodes.Front.Shared.Components.NodeViews;

public partial class NodeComponent
{
    [Parameter] public Node Node { get; set; } = default!;

    float X => Node.X + 10;
    float Y => Node.Y + 8;

    float _bodyRectHeight => Node.BodyRectHeight;
    float _bodyRectWidth => FixedWidth ?? Node.BodyRectWidth;

    [Parameter] public EventCallback<NodeComponentMouseEventArgs> OnMouseDown { get; set; }
    [Parameter] public EventCallback<NodeComponentMouseEventArgs> OnMouseUp { get; set; }

    [Parameter] public EventCallback<NodeWirePointEventArgs> wireStartNew { get; set; }
    [Parameter] public EventCallback<NodeWirePointEventArgs> wireStartNewEnd { get; set; }

    [Parameter] public EventCallback<string> OnInject { get; set; }
    [Parameter] public EventCallback<NodeComponentMouseEventArgs> OnClick { get; set; }
    [Parameter] public EventCallback<NodeComponentMouseEventArgs> OnDblClick { get; set; }

    [Parameter] public float? FixedWidth { get; set; }
    [Parameter] public EventCallback<NodeComponentMouseEventArgs> OnContextMenu { get; set; }
    [Parameter] public bool ShowLabelInsteadDisplayName { get; set; }

    [Parameter] public RenderFragment<NodeComponent>? ChildContent { get; set; } = default!;

    string IconUrl
    {
        get
        {
            string iconUrl = "_content/Mars.Nodes.Workspace/nodes/function.svg";
            if (!string.IsNullOrEmpty(Node.Icon))
            {
                iconUrl = Node.Icon;
            }
            return iconUrl;
        }
    }

    string DisplayTitle => (ShowLabelInsteadDisplayName ? Node.Label : Node.DisplayName).TextEllipsis((int)_bodyRectWidth / 9);

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
        if (Node.Disabled) return;
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

    void OnInputWirePointDown(MouseEventArgs e, int index)
    {
        wireStartNew.InvokeAsync(new NodeWirePointEventArgs(e, index, true, Node));
    }

    void OnInputWirePointUp(MouseEventArgs e, int index)
    {
        wireStartNewEnd.InvokeAsync(new NodeWirePointEventArgs(e, index, true, Node));
    }

    void OnOutputWirePointDown(MouseEventArgs e, int index)
    {
        wireStartNew.InvokeAsync(new NodeWirePointEventArgs(e, index, false, Node));
    }

    void OnOutputWirePointUp(MouseEventArgs e, int index)
    {
        wireStartNewEnd.InvokeAsync(new NodeWirePointEventArgs(e, index, false, Node));
    }

    [Parameter] public Type? ExtendType { get; set; }
}
