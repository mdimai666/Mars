using Mars.Nodes.Core.Nodes.Common;
using Mars.Nodes.Front.Shared.Editor.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Mars.Nodes.Front.Shared.Components.NodeViews;

public partial class CommentNodeComponent
{
    public const int MaxTextLength = 1000;

    [Parameter] public CommentNode Node { get; set; } = default!;

    float X => Node.X + 10;
    float Y => Node.Y + 8;

    public float _bodyRectHeight => Node.BodyRectHeight;
    public float _bodyRectWidth => FixedWidth ?? Node.BodyRectWidth;

    [Parameter] public EventCallback<NodeComponentMouseEventArgs> OnMouseDown { get; set; }
    [Parameter] public EventCallback<NodeComponentMouseEventArgs> OnMouseUp { get; set; }

    [Parameter] public EventCallback<NodeComponentMouseEventArgs> OnClick { get; set; }
    [Parameter] public EventCallback<NodeComponentMouseEventArgs> OnDblClick { get; set; }

    [Parameter] public float? FixedWidth { get; set; }
    [Parameter] public EventCallback<NodeComponentMouseEventArgs> OnContextMenu { get; set; }

    string IconUrl
    {
        get
        {
            string iconUrl = "_content/Mars.Nodes.Workspace/nodes/chat.svg";
            if (!string.IsNullOrEmpty(Node.Icon))
            {
                iconUrl = Node.Icon;
            }
            return iconUrl;
        }
    }

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

}
