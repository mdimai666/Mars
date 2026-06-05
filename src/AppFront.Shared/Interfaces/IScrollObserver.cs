using Microsoft.JSInterop;

namespace AppFront.Shared.Interfaces;

public interface IScrollObserver
{
    [JSInvokable]
    public void OnElementScroll(int scrollTop,
                                int scrollLeft,
                                int scrollHeight,
                                int clientHeight,
                                int scrollWidth,
                                int clientWidth);
}
