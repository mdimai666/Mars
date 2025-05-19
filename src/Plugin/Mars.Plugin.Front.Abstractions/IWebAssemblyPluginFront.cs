using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Mars.Plugin.Front.Abstractions;

public interface IWebAssemblyPluginFront
{
    void ConfigureServices(WebAssemblyHostBuilder builder);
    void ConfigureApplication(WebAssemblyHost app);
}
