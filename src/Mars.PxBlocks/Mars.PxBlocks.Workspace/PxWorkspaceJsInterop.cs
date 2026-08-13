using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Mars.PxBlocks.Workspace;

public class PxWorkspaceJsInterop : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    public PxWorkspaceJsInterop(IJSRuntime jsRuntime)
    {
        _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/Mars.PxBlocks.Workspace/dist/PxBlocks.js").AsTask());
    }

    public async ValueTask<IJSObjectReference> InjectWorkspace(ElementReference element, string? optionsJson = null, string? toolboxJson = null)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<IJSObjectReference>("injectWorkspace", element, optionsJson, toolboxJson);
    }

    public async ValueTask UpdateToolbox(IJSObjectReference workspace, string toolboxJson)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("updateToolbox", workspace, toolboxJson);
    }

    public async ValueTask<bool> SelectCategory(IJSObjectReference workspace, string name)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<bool>("selectCategory", workspace, name);
    }

    public async ValueTask ClearToolboxSelection(IJSObjectReference workspace)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("clearToolboxSelection", workspace);
    }

    public async ValueTask<bool> IsFlyoutVisible(IJSObjectReference workspace)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<bool>("isFlyoutVisible", workspace);
    }

    public async ValueTask SetTypes(string typesJson)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("setTypes", typesJson);
    }

    public async ValueTask RegisterBlockDefinitions(string definitionsJson)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("registerBlockDefinitions", definitionsJson);
    }

    public async ValueTask<string> SaveWorkspace(IJSObjectReference workspace)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<string>("saveWorkspace", workspace);
    }

    public async ValueTask LoadWorkspace(IJSObjectReference workspace, string blocksJson)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("loadWorkspace", workspace, blocksJson);
    }

    public async ValueTask ClearWorkspace(IJSObjectReference workspace)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("clearWorkspace", workspace);
    }

    public async ValueTask Undo(IJSObjectReference workspace, bool redo)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("undo", workspace, redo);
    }

    public async ValueTask CenterContent(IJSObjectReference workspace)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("centerContent", workspace);
    }

    public async ValueTask RegisterEvents(IJSObjectReference workspace, DotNetObjectReference<PxBlocksWorkspace> reference)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("registerEvents", workspace, reference);
    }

    public async ValueTask DisposeWorkspace(IJSObjectReference workspace)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("disposeWorkspace", workspace);
    }

    public async ValueTask<string> GetVersion()
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<string>("getVersion");
    }

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;
            await module.DisposeAsync();
        }
    }
}
