using Mars.Nodes.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Mars.Nodes.Workspace;

public partial class NodeComponent
{
    [Parameter] public Node node { get; set; } = default!;

    [Parameter] public float x { get; set; }
    [Parameter] public float y { get; set; }

    float X => node.X + 10;
    float Y => node.Y + 8;

    public float bodyRectHeight => node.Outputs.Count < 2 ? 30 : node.Outputs.Count * 16f;
    //public float bodyRectWidth => 120;
    public float bodyRectWidth => Math.Min(360, Math.Max(120, node.DisplayName.Length * 9 + 40));

    [Parameter] public EventCallback<MouseEventArgs> OnMouseDown { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnMouseUp { get; set; }

    [Parameter] public EventCallback<NodeWirePointEventArgs> wireStartNew { get; set; }
    [Parameter] public EventCallback<NodeWirePointEventArgs> wireStartNewEnd { get; set; }

    [Parameter] public EventCallback<string> OnInject { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnDblClick { get; set; }

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
        OnInject.InvokeAsync(this.node.Id);
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
