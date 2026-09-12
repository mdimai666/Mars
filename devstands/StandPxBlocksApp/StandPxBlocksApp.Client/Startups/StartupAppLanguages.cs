using System.Globalization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace StandPxBlocksApp.Client.Startups;

internal static class StartupAppLanguages
{
    internal static void ConfigureAppLanguage(this WebAssemblyHostBuilder builder)
    {
        var cultureInfo = new CultureInfo("ru-RU");
        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
        Thread.CurrentThread.CurrentCulture = cultureInfo;
        Thread.CurrentThread.CurrentUICulture = cultureInfo;
    }
}
