using System.Text;
using Microsoft.JSInterop;

namespace AppFront.Shared;

public class AppFrontJs : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> moduleTask;
    private readonly IJSRuntime _js;

    public AppFrontJs(IJSRuntime jsRuntime)
    {
        moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/mdimai666.Mars.AppFront.Shared/AppFront.SharedJsInterop.js").AsTask());
        _js = jsRuntime;
    }

    public async ValueTask<string> Prompt(string message)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<string>("showPrompt", message);
    }

    public ValueTask CopyToClipboard(string text)
    {
        return _js.InvokeVoidAsync("navigator.clipboard.writeText", text);
    }

    public Task DownloadContentAsFile(string content, string filename)
    {
        byte[] fileBytes = Encoding.UTF8.GetBytes(content);
        return DownloadContentAsFile(fileBytes, filename);
    }

    public async Task DownloadContentAsFile(byte[] fileBytes, string filename)
    {
        var module = await moduleTask.Value;

        using var fileStream = new MemoryStream(fileBytes);
        using var streamRef = new DotNetStreamReference(fileStream);
        await module.InvokeVoidAsync("downloadFileFromStream", filename, streamRef);
    }

    public async Task DownloadFileFromUrl(string url)
    {
        var module = await moduleTask.Value;

        await module.InvokeVoidAsync("downloadFileFromUrl", "download", url);
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
