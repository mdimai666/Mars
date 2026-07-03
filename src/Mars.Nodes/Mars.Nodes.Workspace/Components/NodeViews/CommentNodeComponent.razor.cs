using Mars.Nodes.Core.Nodes.Common;
using Mars.Nodes.Workspace.EditorParts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Mars.Nodes.Workspace.Components.NodeViews;

public partial class CommentNodeComponent
{
    public const int MaxTextLength = 1000;

    [Parameter] public CommentNode node { get; set; } = default!;

    [Parameter] public float x { get; set; }
    [Parameter] public float y { get; set; }

    float X => node.X + 10;
    float Y => node.Y + 8;

    public float bodyRectHeight => Math.Max(30, Math.Min(200, (node.Text.Length * 120) / 190));
    //public float bodyRectWidth => 120;
    public float bodyRectWidth => FixedWidth ?? CalcBodyWidth(node);

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
            if (!string.IsNullOrEmpty(node.Icon))
            {
                iconUrl = node.Icon;
            }
            return iconUrl;
        }
    }

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

    public static float CalcBodyWidth(CommentNode node) => Math.Min(360, Math.Max(120, node.Text.Length * 9 + 40));
}
