using System.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;

namespace AppAdmin.Startups;

public static class StrartupSafeModeHelper
{
    public static ValueTask<string> ReadUrlFromBuilder(this WebAssemblyHostBuilder builder)
    {
        var js = (IJSInProcessRuntime)builder.Services.BuildServiceProvider().GetRequiredService<IJSRuntime>();
        return js.InvokeAsync<string>("eval", "window.location.href");
    }

    public static async ValueTask<bool> DetectIsSafeMode(this WebAssemblyHostBuilder builder, ILogger logger)
    {
        var url = await builder.ReadUrlFromBuilder();
        var uri = new Uri(url);
        var safeValue = HttpUtility.ParseQueryString(uri.Query)["safe"];

        bool isSafe = safeValue == "1" || string.Equals(safeValue, "true", StringComparison.OrdinalIgnoreCase);

        if (isSafe) logger.LogWarning("SAFE MODE ON");
        return isSafe;
    }
}
