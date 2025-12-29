using Microsoft.JSInterop;

namespace Mars.Nodes.Workspace.Interfaces;

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

public readonly record struct ScrollInfo(
    int ScrollTop,
    int ScrollLeft,
    int ScrollHeight,
    int ClientHeight,
    int ScrollWidth,
    int ClientWidth
)
{
    public bool IsAtBottom =>
        ScrollTop + ClientHeight >= ScrollHeight - 1;
}
