using Microsoft.JSInterop;

namespace MarsCodeEditor2
{
    // This class provides an example of how JavaScript functionality can be wrapped
    // in a .NET class for easy consumption. The associated JavaScript module is
    // loaded on demand when first needed.
    //
    // This class can be registered as scoped DI service and then injected into Blazor
    // components for use.

    public class MarsCodeEditor2JsInterop : IAsyncDisposable
    {
        private readonly Lazy<Task<IJSObjectReference>> moduleTask;

        public MarsCodeEditor2JsInterop(IJSRuntime jsRuntime)
        {
            moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./_content/mdimai666.Mars.MarsCodeEditor2/MarsCodeEditor2JsInterop.js").AsTask());
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

        public async ValueTask Editor_DoAction(string blazorMonacoId, string actionId)
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("f_editor_doaction", blazorMonacoId, actionId);
        }

        public async ValueTask Editor_activateJSextensions(string blazorMonacoId)
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("activateJSextensions", blazorMonacoId);
        }

        public async ValueTask Editor_setModelLanguage(string blazorMonacoId, string lang)
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("setModelLanguage", blazorMonacoId, lang);
        }
    }
}
