using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Mars.Nodes.Workspace.Components;

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

    [Parameter] public EventCallback<MouseEventArgs> OnMouseDown { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnMouseUp { get; set; }

    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnDblClick { get; set; }

    [Parameter] public float? FixedWidth { get; set; }

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
        OnMouseDown.InvokeAsync(e);
    }
    void OnMouseUpMethod(MouseEventArgs e)
    {
        //OnInputWirePointUp(e);
        //OnOutputWirePointUp(e, 0);
        OnMouseUp.InvokeAsync(e);
    }


    // Simple events ============================
    void OnClickEvent(MouseEventArgs e)
    {
        OnClick.InvokeAsync(e);
    }
    void OnDblClickEvent(MouseEventArgs e)
    {
        OnDblClick.InvokeAsync(e);
    }

    public static float CalcBodyWidth(CommentNode node) => Math.Min(360, Math.Max(120, node.Text.Length * 9 + 40));
}
