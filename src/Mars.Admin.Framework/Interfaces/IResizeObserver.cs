using Microsoft.JSInterop;

namespace Mars.Admin.Framework.Interfaces;

public interface IResizeObserver
{
    [JSInvokable]
    public void OnElementResize(double width, double height);
}
