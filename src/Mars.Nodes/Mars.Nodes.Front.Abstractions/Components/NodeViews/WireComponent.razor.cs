using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Mars.Nodes.Front.Abstractions.Components.NodeViews;

public partial class WireComponent
{
    [Parameter]
    public float X1 { get; set; }
    [Parameter]
    public float Y1 { get; set; }
    [Parameter]
    public float X2 { get; set; }
    [Parameter]
    public float Y2 { get; set; }

    public string Path => FormattableString.Invariant($"M {X1} {Y1} C {X1 + 75} {Y1} {X2 - 75} {Y2} {X2} {Y2}");
    [Parameter]
    public bool Selected { get; set; }
    [Parameter]
    public bool Disable { get; set; }

    [Parameter]
    public EventCallback<MouseEventArgs> OnMouseDown { get; set; }
    [Parameter]
    public EventCallback<MouseEventArgs> OnMouseUp { get; set; }

    [Parameter] public EventCallback<MouseEventArgs> OnContextMenu { get; set; }
    void OnMouseDownMethod(MouseEventArgs e)
    {
        OnMouseDown.InvokeAsync(e);
    }
    void OnMouseUpMethod(MouseEventArgs e)
    {
        OnMouseUp.InvokeAsync(e);
    }
}
