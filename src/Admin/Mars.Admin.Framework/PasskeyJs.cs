using Flurl.Http;
using Mars.Contracts.Common;
using Mars.Identity.Contracts.Auth;
using Microsoft.JSInterop;

namespace Mars.Admin.Framework;

public class PasskeyJs : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> moduleTask;
    private readonly IFlurlClient _client;

    public PasskeyJs(IJSRuntime jsRuntime, IFlurlClient client)
    {
        moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/mdimai666.Mars.Admin.Framework/PasskeyJsInterop.js").AsTask());
        _client = client;
    }

    public async Task<bool> IsAvailable()
    {
        try
        {
            var module = await moduleTask.Value;
            return await module.InvokeAsync<bool>("isAvailable");
        }
        catch (JSException)
        {
            return false;
        }
    }

    public async Task<UserActionResult> RegisterPasskey(string? name)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<UserActionResult>("registerPasskey",
            ApiUrl("creation-options"), ApiUrl("register"), name);
    }

    public async Task<AuthResultResponse> LoginWithPasskey()
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<AuthResultResponse>("loginWithPasskey",
            ApiUrl("request-options"), ApiUrl("login"));
    }

    string ApiUrl(string action) => $"{_client.BaseUrl.ToString().TrimEnd('/')}/api/passkey/{action}";

    public async ValueTask DisposeAsync()
    {
        if (moduleTask.IsValueCreated)
        {
            var module = await moduleTask.Value;
            await module.DisposeAsync();
        }
    }
}
