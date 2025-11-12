using System.Globalization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace AppAdmin.Startups;

internal static class StartupAppLanguages
{
    internal static void ConfigureAppLanguage(this WebAssemblyHostBuilder builder)
    {
        var defaultCulture = new CultureInfo("ru");
        var cultureInfo = defaultCulture;
        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
        Thread.CurrentThread.CurrentCulture = cultureInfo;
        Thread.CurrentThread.CurrentUICulture = cultureInfo;
    }
}
