using Mars.Nodes.Core;
using Mars.Nodes.Workspace.EditorParts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Mars.Nodes.Workspace.Components;

public partial class NodeComponent
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

    [Parameter] public EventCallback<MouseEventArgs> OnMouseDown { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnMouseUp { get; set; }

    [Parameter] public EventCallback<NodeWirePointEventArgs> wireStartNew { get; set; }
    [Parameter] public EventCallback<NodeWirePointEventArgs> wireStartNewEnd { get; set; }

    [Parameter] public EventCallback<string> OnInject { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnDblClick { get; set; }

    [Parameter] public float? FixedWidth { get; set; }

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

    void OnInjectClick(MouseEventArgs e)
    {
        OnInject.InvokeAsync(node.Id);
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
}
