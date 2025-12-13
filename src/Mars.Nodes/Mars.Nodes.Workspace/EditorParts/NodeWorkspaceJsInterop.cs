using System;
using System.Drawing;
using System.Threading.Tasks;
using Mars.Nodes.Workspace.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Mars.Nodes.Workspace.EditorParts
{
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

        public async ValueTask ObserveAsync(ElementReference element, DotNetObjectReference<IResizeObserver> callback)
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync(
                "observeSize",
                element,
                callback
            );
        }

        public async ValueTask UnobserveAsync(ElementReference element)
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("unobserveSize", element);
        }
    }
}
