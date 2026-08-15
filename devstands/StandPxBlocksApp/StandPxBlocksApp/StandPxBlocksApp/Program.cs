using Blazored.LocalStorage;
using Mars.PxBlocks.Host;
using Mars.PxBlocks.Host.Hubs;
using Mars.PxBlocks.Host.Shared;
using Mars.PxBlocks.Host.Shared.Services;
using Mars.PxBlocks.Runtime.Ast;
using StandPxBlocksApp.Blocks;
using StandPxBlocksApp.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
//builder.Services.AddHttpClient<IFlurlClient, FlurlClient>();

// Регистрация нужна и на сервере для пререндера страницы; реально сервис используется только в WASM.
builder.Services.AddBlazoredLocalStorage();

builder.Services.AddCors(options => //not check
{
    options.AddDefaultPolicy(
        builder => builder
            .AllowAnyOrigin()
            .AllowAnyHeader()
    );
});

builder.Services.AddControllers();

// Серверное исполнение PxBlocks: api/PxBlocks + SignalR-стриминг событий.
builder.Services.AddPxBlocks();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

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
app.MapControllers();

// Ядерные определения (Start/Loop) + демо-домен стенда: определения и реализации
// живут только на сервере, редактор получает их через api/PxBlocks/Definitions.
app.UsePxBlocks();
var pxCatalog = app.Services.GetRequiredService<IPxBlockCatalog>();
pxCatalog.RegisterAssembly(typeof(Program).Assembly);
pxCatalog.RegisterToolboxCategory(PxDemoToolbox.CreateCategory());

// Контекст демо-домена: те же блоки, но редактор получает их по имени контекста
// (api/PxBlocks/Contexts/demo), а запуск идёт с политикой контекста.
var pxContexts = app.Services.GetRequiredService<IPxEditorContextRegistry>();
pxContexts.Register(PxEditorContext.Define("demo")
    .Title("Демо-домен")
    .Description("Демо-блоки стенда: типы стыковок, объекты, события Start/Loop")
    .Events(PxEvents.Start, PxEvents.Loop)
    .Set<PxDemoBlocks>()
    .Category(PxDemoToolbox.CreateCategory()));

app.MapHub<PxBlocksHub>(PxBlocksConstants.HubRoute);

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(StandPxBlocksApp.Client._Imports).Assembly);

app.Run();
