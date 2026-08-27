using Mars.Admin.Framework;
using Flurl.Http;
using Mars.Nodes.Workspace;
using Mars.Contracts.XActions;
using Mars.Contracts.Options;
using Mars.Contracts.ViewModels;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using StandNodesApp.Client.Startups;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var backendUrl = builder.HostEnvironment.BaseAddress.TrimEnd('/');

var httpClient = new HttpClient() { BaseAddress = new Uri(backendUrl) };
builder.Services.AddScoped(sp => httpClient);
builder.Services.AddScoped<IFlurlClient>(sp => new FlurlClient(httpClient));

builder.Services.AddLocalization();
builder.ConfigureAppLanguage();

builder.ConfigureWebSockets(backendUrl);
builder.Services.AddAppFrontMain(builder.Configuration, typeof(Program));
builder.Services.AddNodeWorkspace();

var vm = new InitialSiteDataViewModel()
{
    NavMenus = [],
    Options = [],
    PostTypes = [],
    SysOptions = new SysOptions(),
    UserPrimaryInfo = null,
    XActions = new Dictionary<string, XActionCommand>(),
};
Q.UpdateInitialSiteData(vm);

var app = builder.Build();

app.Services.UseNodeWorkspace();

await app.RunAsync();
