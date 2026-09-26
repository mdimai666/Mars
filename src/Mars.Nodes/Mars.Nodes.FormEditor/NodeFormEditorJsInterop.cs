using System.Drawing;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Mars.Nodes.FormEditor;

public class NodeFormEditorJsInterop : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    public NodeFormEditorJsInterop(IJSRuntime jsRuntime)
    {
        moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
           "import", "./_content/mdimai666.Mars.Nodes.FormEditor/nodeFormJsInterop.js").AsTask());
    }

    public async ValueTask<string> Prompt(string message)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<string>("showPrompt", message);
    }

    public async ValueTask DisposeAsync()
    {
        if (moduleTask.IsValueCreated)
        {
            var module = await moduleTask.Value;
            await module.DisposeAsync();
        }
    }

    public async ValueTask ShowOffcanvas(string htmlId, bool open = true)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("ShowOffcanvas", htmlId, open);
    }

    public async ValueTask<PointF> HtmlGetElementScroll(string selector)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<PointF>("HtmlGetElementScroll", selector);
    }

    public async ValueTask SubscribeOffcanvasHide(string selector, object dotnetRefHelper, string methodName)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("Offcanves_events_subscribe_hide", selector, dotnetRefHelper, methodName);
    }

    public async ValueTask Editor_DoAction(string actionId)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("f_editor_doaction", actionId);
    }

    public async ValueTask<CaretRange> ValueInput_GetSelection(ElementReference element)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<CaretRange>("mvi_getSelection", element);
    }

    public async ValueTask ValueInput_SetCaret(ElementReference element, int position)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("mvi_setCaret", element, position);
    }

    public async ValueTask ValueInput_Bind(ElementReference root, ElementReference input, object dotNetRef,
                                          string outsideClickMethod, string pasteMethod)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("mvi_bind", root, input, dotNetRef, outsideClickMethod, pasteMethod);
    }

    public async ValueTask ValueInput_PopupOpen(ElementReference popup, ElementReference anchor, string align)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("mvi_popupOpen", popup, anchor, align);
    }

    public async ValueTask ValueInput_Dispose(ElementReference root, ElementReference input, ElementReference anchor)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("mvi_dispose", root, input, anchor);
    }
}

public record struct CaretRange(int Start, int End);
