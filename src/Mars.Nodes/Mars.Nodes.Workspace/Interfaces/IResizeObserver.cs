using Microsoft.JSInterop;

namespace Mars.Nodes.Workspace.Interfaces;

public interface IResizeObserver
{
    [JSInvokable]
    public void OnElementResize(double width, double height);
}
