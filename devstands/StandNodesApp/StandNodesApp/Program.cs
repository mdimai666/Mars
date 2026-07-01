using Flurl.Http;
using Mars.Host.Shared.Hubs;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Startup;
using Mars.Nodes.Host;
using Mars.Nodes.Workspace;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.FluentUI.AspNetCore.Components;
using StandNodesApp;
using StandNodesApp.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddHttpClient<IFlurlClient, FlurlClient>();

builder.Services.AddCors(options => //not check
{
    options.AddDefaultPolicy(
        builder => builder
            .AllowAnyOrigin()
            .AllowAnyHeader()
    );
});

builder.ConfigureAddAuthenticationMockServices();

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.HostConfigureServices();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddFluentUIComponents();

builder.Services.AddNodeWorkspace();
builder.Services.AddMarsNodes();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseCors();
app.UseRouting();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.HostConfigure();
app.MapHub<ChatHub>("/_ws/admin", options =>
{
    options.Transports = HttpTransportType.WebSockets | HttpTransportType.LongPolling;
});

app.UseMarsNodes();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(StandNodesApp.Client._Imports).Assembly);

MarsLogger.Initialize(app.Services.GetRequiredService<ILoggerFactory>());
IMarsAppLifetimeService.UseAppLifetime(builder.Services, app);

app.Run();
