using System.Text;
using MarsDocs.DevServer.Components;
using MarsDocs.DevServer.Services;
using MarsDocs.DevServer.Startups;
using Microsoft.FluentUI.AspNetCore.Components;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddHttpClient();
builder.Services.AddFluentUIComponents();

builder.Services.AddSingleton<SseManager>();
builder.Services.AddHostedService<MdWatcher>();

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

app.UseRouting();
app.UseAntiforgery();

app.UseServeMarkdownFiles();
app.UseMarkdownFilesReloadSse();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(MarsDocs.WebApp._Imports).Assembly);

app.Run();
