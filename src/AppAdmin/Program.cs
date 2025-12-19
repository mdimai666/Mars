using AppAdmin;
using AppAdmin.Components;
using AppAdmin.Startups;
using AppFront.Main.Extensions;
using AppFront.Main.OptionEditForms;
using AppFront.Shared.Features;
using AppFront.Shared.Interfaces;
using Mars.Datasource.Front;
using Mars.Nodes.Workspace;
using Mars.Options.Front;
using Mars.Plugin.Front;
using Mars.SemanticKernel.Front;
using Mars.WebApp.Nodes.Front;
using MarsCodeEditor2;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Toolbelt.Blazor.Extensions.DependencyInjection;

//Info: Для быстрой разработки через dotnet watch запускайте Dev/DevAdmin.DevServer

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Logging.SetMinimumLevel(LogLevel.Trace);
builder.Logging.AddFilter("System", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft", LogLevel.Error);

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

builder.ConfigureAppLanguage();
builder.Services.AddAppFrontMain(builder.Configuration, typeof(Program));

Q.WorkDir = "C:\\Users\\D\\Documents\\VisualStudio\\2025\\Mars\\src\\";
Q.SetupHostingInfo(new BackendHostingInfo { Backend = new Uri(Q.BackendUrl) });
CodeEditor2.ToolbarComponents.Add(typeof(CodeEditorExtraToolbar));
ContentWrapper.GeneralSectionActions = typeof(AppAdmin.Shared.GeneralSectionActions);

logger.LogTrace("Adding workspace services...");
builder.Services.AddHotKeys2();
builder.Services.AddNodeWorkspace()
                .AddMarsWebAppNodesFront()
                .AddDatasourceWorkspace()
                .AddSemanticKernelFront();

if (!App.IsPrerenderProcess)
    builder.ConfigureWebSockets(backendUrl);

logger.LogTrace("Loading remote plugin assemblies");
await builder.AddRemotePluginAssemblies(Q.BackendUrl);

logger.LogTrace("Building application...");
var app = builder.Build();

logger.LogTrace("Initializing services...");
app.Services.UseAppFrontMain()
            .UseNodeWorkspace()
            .UseMarsWebAppNodesFront()
            .UseDatasourceWorkspace()
            .UseSemanticKernelFront();

var optionsFormsLocator = app.Services.GetRequiredService<OptionsFormsLocator>();
optionsFormsLocator.RegisterAssembly(typeof(ApiOptionEditForm).Assembly);

SmartSaveExtensions.Setup(app.Services.GetRequiredService<IMessageService>());

logger.LogTrace("Using remote plugin assemblies...");
app.UseRemotePluginAssemblies();

logger.LogTrace("=== Application startup complete, running... ===");
await app.RunAsync();
