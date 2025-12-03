using AppFront.Shared;
using AppFront.Shared.Features;
using BlazorTemplateExample;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var logger = builder.Logging.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
logger.LogTrace("=== Application startup begin ===");

string? backendUrl = builder.Configuration["BackendUrl"];
if (string.IsNullOrEmpty(backendUrl))
{
    backendUrl = builder.HostEnvironment.BaseAddress.TrimEnd('/').Replace("/dev", "", StringComparison.OrdinalIgnoreCase).TrimEnd('/');
    if (backendUrl == "http://localhost:5185") backendUrl = "http://localhost:5003";
    logger.LogTrace("BackendUrl from BaseAddress: {BackendUrl}", backendUrl);
    Q.BackendUrl = backendUrl;
}
builder.Services.AddAppFront(builder.Configuration, typeof(Program));

Q.WorkDir = "Mars\\src\\";
Q.SetupHostingInfo(new BackendHostingInfo { Backend = new Uri(Q.BackendUrl) });

logger.LogTrace("Building application...");
var app = builder.Build();

logger.LogTrace("=== Application startup complete, running... ===");
await app.RunAsync();
