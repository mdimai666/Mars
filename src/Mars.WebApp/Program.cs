using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using AppFront.Shared;
using Mars.CommandLine;
using Mars.Datasource.Front;
using Mars.Datasource.Host;
using Mars.Docker.Host;
using Mars.Excel.Host;
using Mars.Host;
using Mars.Host.Shared.Features;
using Mars.Host.Shared.Hubs;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Startup;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements;
using Mars.Nodes.Host;
using Mars.Nodes.Workspace;
using Mars.Options.Host;
using Mars.Plugin;
using Mars.Scheduler.Host;
using Mars.SemanticKernel.Host;
using Mars.UseStartup;
using Mars.UseStartup.MarsParts;
using Mars.WebSiteProcessor;
using Mars.XActions;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using static Mars.UseStartup.MarsStartupInfo;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;
var startWatch = new Stopwatch();
startWatch.Start();

_ = nameof(MarsStartupInfo);

// todo: some fix for run from not Mars directory
//var marsAssemblyPath = System.Reflection.Assembly.GetEntryAssembly().Location;
//var wd = Path.GetDirectoryName(MarsAssemblyPath);
//Directory.SetCurrentDirectory(wd);

#if DEBUG
//FIX for NET7 AppAdmin serve WebAssembly files
if (Environment.GetEnvironmentVariable("DOTNET_WATCH") == "1")
{
    Console.WriteLine("DOTNET_WATCH");
    Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
}

//Debugger.IsAttached
if (!IsRunUnderVisualStudio)
{
    Directory.SetCurrentDirectory(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
    Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
}

#endif

var commandsApi = new CommandLineApi();

var builder = WebApplication.CreateBuilder(args);
if (!IsTesting && !IsRunningInDocker)
{
    builder.ConfigureAppConfiguration(args);
}
builder.Services.AddFeatureManagement();
builder.Services.MarsAddLocalization()
                .MarsAddCore(builder.Configuration)
                .MarsAddMetrics()
                .AddConfigureActions()
                .AddMarsWebSiteProcessor();
builder.AddFront();

builder.Services.AddDateOnlyTimeOnlyStringConverters()
                .AddResponseCaching()
                .AddMemoryCache(options =>
                {
                    options.TrackStatistics = true;
                })
                .AddLogging();

builder.Services.AddResponseCompression(opts =>
{
    opts.Providers.Add<BrotliCompressionProvider>();
    opts.Providers.Add<GzipCompressionProvider>();
    opts.EnableForHttps = true;
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]);
})
    .Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal)
    .Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal);

builder.Services.AddControllers();

//------------------------------------------
// SignalR

builder.Services
    .AddSignalR(hubOptions =>
    {
        hubOptions.EnableDetailedErrors = true;
        hubOptions.KeepAliveInterval = TimeSpan.FromMinutes(1);
    })
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = null;
    });

//------------------------------------------
// Razor page

builder.Services.AddRazorPages();
//builder.Services.AddServerSideBlazor();

//TODO: только для определенных эндпоинтов
builder.Services.Configure<FormOptions>(x =>
{
    x.MultipartBodyLengthLimit = 200 * 1024 * 1024;
});
builder.Services.Configure<IHttpMaxRequestBodySizeFeature>(x =>
{
    x.MaxRequestBodySize = 200 * 1024 * 1024;
});

//------------------------------------------
// Logger
bool disableLogs = commandsApi.CheckGlobalOption<bool>("--disable-logs", args);
if (!disableLogs && !IsTesting)
{
    builder.MarsAddLogging();
}

//------------------------------------------
// Mars
builder.Services.MarsAddSwagger()
                .AddMarsOptions()
                .MarsAddServices(builder.Environment)
                .MarsAddTemplator()
                .AddDevAdmin()
                .AddMarsNodes()
                .AddDatasourceHost()
                .AddMarsExcel()
                .AddMarsScheduler()
                .AddMarsDocker()
                .AddMarsSemanticKernel();

//------------------------------------------
// CLIENT
#if !NOADMIN
builder.Services.AddAppFrontMain(builder.Configuration, typeof(AppAdmin.Program));
#endif
builder.Services.AddNodeWorkspace();
builder.Services.DatasourceWorspace();

NodesLocator.RefreshDict();
NodeFormsLocator.RefreshDict();
NodeImplementFabirc.RefreshDict();
// end CLIENT

//------------------------------------------
// PLUGINS
builder.AddPlugins()
        .Services.AddControllers().AddPluginsAsPartOfMvc();//warn: need for AddPlugins

// ===========================================================================================
// APP
// ===========================================================================================

var app = builder.Build();

var _logger = app.Services.GetRequiredService<ILogger<Program>>();
MarsLogger.Initialize(app.Services.GetRequiredService<ILoggerFactory>()); // use like: MarsLogger.GetStaticLogger<T>().LogError(...)
var env = app.Services.GetRequiredService<IWebHostEnvironment>();
//var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

commandsApi.Setup(app);

if (env.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    //app.UseDatabaseErrorPage();//deplrecated
    app.UseMigrationsEndPoint();
    app.UseWebAssemblyDebugging();

}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    //app.UseHsts();
}

await commandsApi.InvokeBaseCommands(IsTesting ? [] : args);
if (!commandsApi.IsContinueRun) return 0;

//Hello message
Console.WriteLine(Mars.Core.Extensions.MarsStringExtensions.HelloText());

commandsApi.GetCommand<MainCommand>().ShowInfoCommand();

app.Services.MarsMigrateIfProducation(builder.Configuration, _logger, out var migrated);
app.Services.UseMarsOptions();
app.Services.SeedData(builder.Configuration, _logger, migrated);

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.MarsUseLocalization();

if (!IsDevelopment)
{
    app.UseResponseCompression();
    app.UseResponseCaching();
}

app.UseCors();
//app.UseIdentityServer();
app.UseRouting(); //11-22
app.UseAuthentication(); //11-22
#pragma warning disable ASP0001 // Authorization middleware is incorrectly configured
app.UseAuthorization(); //11-22
#pragma warning restore ASP0001 // Authorization middleware is incorrectly configured

app.MarsUseSwagger();

//app.UseEndpoints(endpoints => //11-22
//{
//    endpoints.MapControllers();
//});

app.MapControllers();

//11-22 - все с таким комментарием необходимо для запуска.

app.Map("/_ws", ws =>
{
    ws.UseRouting();

    ws.UseEndpoints(endpoints =>
    {
        endpoints.MapHub<ChatHub>("/ws", options =>
        {
            options.Transports =
                HttpTransportType.WebSockets |
                HttpTransportType.LongPolling;
        });
    });
});

//app.MarsUseMetrics();
app.UseMarsHost(builder.Services);
app.UseConfigureActions();
app.MarsUseTemplator();
app.UsePlugins();
NodesLocator.RefreshDict();
NodeFormsLocator.RefreshDict();
NodeImplementFabirc.RefreshDict();
app.UseDevAdmin();
app.UseMarsNodes(); //TODO: запросы на ресурсы тоже ловит AppFront.styles.css appsettings.json, если разрешить Match
NodeServiceTemplaryHelper._serviceCollection = builder.Services;
app.UseDatasourceHost();
app.UseMarsWebSiteProcessor();
app.UseMarsExcel();
app.UseForFeature(FeatureFlags.DockerAgent, app => app.UseMarsDocker());
app.UseForFeature(FeatureFlags.AITool, app => app.UseMarsSemanticKernel());

await commandsApi.InvokeCommands(IsTesting ? [] : args);
if (!commandsApi.IsContinueRun) return 0;

app.UseMarsScheduler();
app.UseFront();

startWatch.Stop();
Console.WriteLine($"start in : {startWatch.ElapsedMilliseconds.ToString("0")}ms");

Console.WriteLine(">RUN");

IMarsAppLifetimeService.UseAppLifetime(builder.Services, app);
app.Run();
return 0;
