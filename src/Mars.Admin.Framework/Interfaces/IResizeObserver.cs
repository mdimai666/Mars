using Microsoft.JSInterop;

namespace AppFront.Shared.Interfaces;

public interface IResizeObserver
{
    [JSInvokable]
    public void OnElementResize(double width, double height);
}
