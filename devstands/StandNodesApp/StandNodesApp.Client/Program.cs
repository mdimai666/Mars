using Flurl.Http;
using Mars.Admin.Contracts.ViewModels;
using Mars.Admin.Framework;
using Mars.Admin.Framework.Features;
using Mars.CodeCompletion.Front;
using Mars.Nodes.Workspace;
using Mars.Server.Contracts.Options;
using Mars.XActions.Contracts;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using StandNodesApp.Client.Startups;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var backendUrl = builder.HostEnvironment.BaseAddress.TrimEnd('/');

var httpClient = new HttpClient() { BaseAddress = new Uri(backendUrl) };
// FlurlClient в конструкторе мутирует httpClient.Timeout; после первого запроса HttpClient
// запрещает менять настройки (net_http_operation_started) — поэтому один инстанс на приложение.
var flurlClient = new FlurlClient(httpClient);
builder.Services.AddScoped(sp => httpClient);
builder.Services.AddScoped<IFlurlClient>(sp => flurlClient);

builder.Services.AddLocalization();
builder.ConfigureAppLanguage();

builder.ConfigureWebSockets(backendUrl);
builder.Services.AddMarsAdminFramework(builder.Configuration, typeof(Program));
builder.Services.AddNodeWorkspace();
builder.Services.AddCodeCompletionFront();

var vm = new InitialSiteDataViewModel()
{
    NavMenus = [],
    Options = [],
    PostTypes = [],
    SiteSettings = new SiteSettings(),
    UserPrimaryInfo = null,
    XActions = new Dictionary<string, XActionCommand>(),
};
Q.UpdateInitialSiteData(vm);

var app = builder.Build();

app.Services.UseNodeWorkspace();

await app.RunAsync();
