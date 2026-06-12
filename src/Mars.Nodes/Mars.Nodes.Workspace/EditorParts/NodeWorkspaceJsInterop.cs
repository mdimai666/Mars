using System.Drawing;
using System.Text.Json.Serialization;
using AppFront.Shared.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Mars.Nodes.Workspace.EditorParts;
// This class provides an example of how JavaScript functionality can be wrapped
// in a .NET class for easy consumption. The associated JavaScript module is
// loaded on demand when first needed.
//
// This class can be registered as scoped DI service and then injected into Blazor
// components for use.

public class NodeWorkspaceJsInterop : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    public NodeWorkspaceJsInterop(IJSRuntime jsRuntime)
    {
        _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
           "import", "./_content/Mars.Nodes.Workspace/nodeWorkspace.js").AsTask());
    }

    public async ValueTask<string> InitModule()
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<string>("InitModule");
    }

    public async ValueTask ScrollDownElement(string selector)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("ScrollDownElement", selector);
    }

    public async ValueTask ShowOffcanvas(string htmlId, bool open = true)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("ShowOffcanvas", htmlId, open);
    }

    public async ValueTask<string> Prompt(string message)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<string>("showPrompt", message);
    }

    public async ValueTask<PointF> HtmlGetElementScroll(string selector)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<PointF>("HtmlGetElementScroll", selector);
    }

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;
            await module.DisposeAsync();
        }
    }

    public async ValueTask ObserveSizeAsync(ElementReference element, DotNetObjectReference<IResizeObserver> callback)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync(
            "observeSize",
            element,
            callback
        );
    }

    public async ValueTask UnobserveSizeAsync(ElementReference element)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("unobserveSize", element);
    }

    public async ValueTask ObserveScrollAsync(ElementReference element, DotNetObjectReference<IScrollObserver> callback)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync(
            "observeScroll",
            element,
            callback
        );
    }

    public async ValueTask UnobserveScrollAsync(ElementReference element)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("unobserveScroll", element);
    }

    public async void TouchFlashAnimation(ElementReference element)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("touchFlashAnimation", element);
    }

    public async void TouchFlashAnimationBySelector(string elementSelector)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("touchFlashAnimationBySelector", elementSelector);
    }

    public async void ScrollToCoordinates(ElementReference element, float x, float y)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("scrollToCoordinates", element, x, y);
    }

    public async void ScrollToCoordinatesBySelector(string elementSelector, float x, float y)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("scrollToCoordinatesBySelector", elementSelector, x, y);
    }

    public async void ScrollToElement(ElementReference element)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("scrollToElement", element);
    }

    public async void ScrollToElementBySelector(string elementSelector)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("scrollToElementBySelector", elementSelector);
    }

    public async ValueTask<ElementBounds?> GetElementBounds(ElementReference element)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<ElementBounds?>("getElementBounds", element);
    }

    public async ValueTask<ElementBounds?> GetElementBoundsBySelector(string elementSelector)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<ElementBounds?>("getElementBoundsBySelector", elementSelector);
    }

    public async ValueTask<ViewportMetrics> GetViewportMetricsAsync()
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<ViewportMetrics>("getViewportMetrics");
    }
}

public struct ElementBounds
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double Top { get; set; }
    public double Right { get; set; }
    public double Bottom { get; set; }
    public double Left { get; set; }
}

public class ViewportMetrics
{
    public double ClientWidth { get; set; }
    public double ClientHeight { get; set; }
    public double InnerWidth { get; set; }
    public double InnerHeight { get; set; }
    public double ScrollWidth { get; set; }
    public double ScrollHeight { get; set; }
    public double PageXOffset { get; set; }
    public double PageYOffset { get; set; }
    public double ScreenWidth { get; set; }
    public double ScreenHeight { get; set; }
    public double DevicePixelRatio { get; set; }
    public int OrientationAngle { get; set; }

    // Вспомогательное свойство для чтения строки из JS (в формате "kebab-case")
    [JsonPropertyName("orientationType")]
    public string RawOrientationType { get; set; } = string.Empty;

    // Удобный C# Enum для работы в коде
    [JsonIgnore]
    public ScreenOrientationType Orientation => RawOrientationType switch
    {
        "portrait-primary" => ScreenOrientationType.PortraitPrimary,
        "portrait-secondary" => ScreenOrientationType.PortraitSecondary,
        "landscape-primary" => ScreenOrientationType.LandscapePrimary,
        "landscape-secondary" => ScreenOrientationType.LandscapeSecondary,
        _ => ScreenOrientationType.Unknown
    };
}

public enum ScreenOrientationType
{
    PortraitPrimary,
    PortraitSecondary,
    LandscapePrimary,
    LandscapeSecondary,
    Unknown
}
