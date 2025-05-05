using Microsoft.JSInterop;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace NodeWorkspace
{
    // This class provides an example of how JavaScript functionality can be wrapped
    // in a .NET class for easy consumption. The associated JavaScript module is
    // loaded on demand when first needed.
    //
    // This class can be registered as scoped DI service and then injected into Blazor
    // components for use.

    public class NodeWorkspaceJsInterop : IAsyncDisposable
    {
        private readonly Lazy<Task<IJSObjectReference>> moduleTask;

        public NodeWorkspaceJsInterop(IJSRuntime jsRuntime)
        {
            moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
               "import", "./_content/NodeWorkspace/nodeWorkspace.js?v=0000 q2").AsTask());
        }

        public async ValueTask<string> InitModule()
        {
            var module = await moduleTask.Value;
            return await module.InvokeAsync<string>("InitModule");
        }

        public async ValueTask ScrollDownElement(string selector)
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("ScrollDownElement", selector);
        }

        public async ValueTask ShowOffcanvas(string htmlId, bool open = true)
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("ShowOffcanvas", htmlId, open);
        }

        public async ValueTask<string> Prompt(string message)
        {
            var module = await moduleTask.Value;
            return await module.InvokeAsync<string>("showPrompt", message);
        }

        public async ValueTask<PointF> HtmlGetElementScroll(string selector)
        {
            var module = await moduleTask.Value;
            return await module.InvokeAsync<PointF>("HtmlGetElementScroll", selector);
        }

        public async ValueTask DisposeAsync()
        {
            if (moduleTask.IsValueCreated)
            {
                var module = await moduleTask.Value;
                await module.DisposeAsync();
            }
        }
    }
}
