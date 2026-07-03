using Mars.Core.Extensions;
using Mars.Nodes.Core;
using Mars.Nodes.Workspace.EditorParts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Mars.Nodes.Workspace.Components.NodeViews;

public partial class LinkNodeComponent
{
    [Parameter] public Node node { get; set; } = default!;

    [Parameter] public float x { get; set; }
    [Parameter] public float y { get; set; }

    float X => node.X + 10;
    float Y => node.Y + 8;

    int inputOrOutputsMax => Math.Max(node.Inputs.Count, node.Outputs.Count);

    public float bodyRectHeight => inputOrOutputsMax < 2 ? 30 : inputOrOutputsMax * 16f;
    //public float bodyRectWidth => 120;
    public float bodyRectWidth => FixedWidth ?? CalcBodyWidth(node);

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

    [CascadingParameter] NodeEditor1 _nodeEditor1 { get; set; } = default!;

    string IconUrl
    {
        get
        {
            string iconUrl = "_content/Mars.Nodes.Workspace/nodes/function.svg";
            if (!string.IsNullOrEmpty(node.Icon))
            {
                iconUrl = node.Icon;
            }
            return iconUrl;
        }
    }

    string DisplayTitle => (ShowLabelInsteadDisplayName ? node.Label : node.DisplayName).TextEllipsis((int)bodyRectWidth / 9);

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
        OnInject.InvokeAsync(node.Id);
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

    void OnInputWirePointDown(MouseEventArgs e, int index)
    {
        wireStartNew.InvokeAsync(new NodeWirePointEventArgs(e, index, true, node));
    }

    void OnInputWirePointUp(MouseEventArgs e, int index)
    {
        wireStartNewEnd.InvokeAsync(new NodeWirePointEventArgs(e, index, true, node));
    }

    void OnOutputWirePointDown(MouseEventArgs e, int index)
    {
        wireStartNew.InvokeAsync(new NodeWirePointEventArgs(e, index, false, node));
    }

    void OnOutputWirePointUp(MouseEventArgs e, int index)
    {
        wireStartNewEnd.InvokeAsync(new NodeWirePointEventArgs(e, index, false, node));
    }

    public static float CalcBodyWidth(Node node) => Math.Min(360, Math.Max(120, node.DisplayName.Length * 9 + 40));
    //public static float CalcBodyWidth(Node node) => 50;
}
