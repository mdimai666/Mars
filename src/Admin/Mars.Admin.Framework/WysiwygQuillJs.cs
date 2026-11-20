using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Mars.Admin.Framework;

public class WysiwygQuillJs : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    public WysiwygQuillJs(IJSRuntime jsRuntime)
    {
        moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", WysiwygQuillAssets.JsModuleUrl).AsTask());
    }

    public async Task InitAsync(ElementReference editor, ElementReference toolbar, string? optionsJson = null)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("init", editor, toolbar, optionsJson);
    }

    public async Task<string?> GetHtmlAsync(ElementReference editor)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<string?>("getHtml", editor);
    }

    public async Task LoadHtmlAsync(ElementReference editor, string html)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("loadHtml", editor, html);
    }

    public async Task InsertImageAsync(ElementReference editor, string url)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("insertImage", editor, url);
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
