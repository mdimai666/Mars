using System.Diagnostics;
using System.Text;
using AppFront.Main.OptionEditForms;
using AppFront.Shared;
using Mars.CommandLine;
using Mars.Datasource.Front;
using Mars.Datasource.Host;
using Mars.Docker.Host;
using Mars.Excel.Host;
using Mars.Host;
using Mars.Host.Shared.Extensions;
using Mars.Host.Shared.Features;
using Mars.Host.Shared.Hubs;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Startup;
using Mars.Nodes.Host;
using Mars.Nodes.Workspace;
using Mars.Options.Front;
using Mars.Options.Host;
using Mars.Plugin;
using Mars.Scheduler.Host;
using Mars.SemanticKernel.Host;
using Mars.SSO;
using Mars.SSO.Host.OAuth;
using Mars.UseStartup;
using Mars.UseStartup.MarsParts;
using Mars.WebApp.Nodes.Host;
using Mars.WebSiteProcessor;
using Mars.XActions;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.HttpOverrides;
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
    builder.Configuration.ConfigureAppConfiguration(args);
}
builder.Services.AddFeatureManagement(builder.Configuration.GetSection(FeatureExtensions.SectionName));
builder.Services.MarsAddLocalization()
                .MarsAddCore(builder.Configuration)
                .AddAspNetTools()
                .MarsAddMetrics(builder.Configuration)
                .AddConfigureActions()
                .AddMarsWebSiteProcessor();
builder.AddFront();

builder.Services.AddControllers();

builder.Services.AddMarsSignalRConfiguration()
                .AddRazorPages();
//builder.Services.AddServerSideBlazor();

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
                .AddMarsHostServices(builder.Environment)
                .MarsAddTemplator()
                .AddDevAdmin()
                .AddMarsNodes()
                .AddMarsWebAppNodes()
                .AddDatasourceHost()
                .AddMarsExcel()
                .AddMarsScheduler();

builder.AddIfFeatureEnabled(FeatureFlags.DockerAgent, b => b.Services.AddMarsDocker());
builder.AddIfFeatureEnabled(FeatureFlags.AITool, b => b.Services.AddMarsSemanticKernel());
builder.AddIfFeatureEnabled(FeatureFlags.SingleSignOn, b => b.Services.AddMarsSSO().AddMarsOAuthHost());

//------------------------------------------
// CLIENT
#if !NOADMIN
builder.Services.AddAppFrontMain(builder.Configuration, typeof(AppAdmin.App));
#endif
builder.Services.AddNodeWorkspace();
builder.Services.AddDatasourceWorkspace();
// end CLIENT

//------------------------------------------
// PLUGINS
builder.AddPlugins();

// ===========================================================================================
// APP
// ===========================================================================================

var app = builder.Build();

var _logger = app.Services.GetRequiredService<ILogger<Program>>();
MarsLogger.Initialize(app.Services.GetRequiredService<ILoggerFactory>()); // use like: MarsLogger.GetStaticLogger<T>().LogError(...)
var env = app.Environment;
//var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

commandsApi.Setup(app);

if (env.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
    app.UseWebAssemblyDebugging();

}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    //app.UseHsts();
}

var (baseCmdInvoked, isHelpCmd) = await commandsApi.InvokeBaseCommands(IsTesting ? [] : args);
if (baseCmdInvoked) return 0;

//Hello message
Console.WriteLine(Mars.Core.Extensions.MarsStringExtensions.HelloText());

if (!isHelpCmd)
{
    commandsApi.GetCommand<InfoCommand>().ShowInfoCommand(showHello: false);
}
app.Services.MarsAutoMigrateCheck(builder.Configuration, _logger, out var migrated);
app.Services.UseMarsHostServices();
app.Services.UseMarsOptions();
app.Services.SeedData(builder.Configuration, _logger, migrated);
app.ApplyPluginMigrations();

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
//app.UseAntiforgery();
app.UseAuthentication(); //11-22
app.UseIfFeatureEnabled(FeatureFlags.SingleSignOn, app => app.UseMarsSSOMiddlewares());
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

app.MarsUseMetrics();
app.UseMarsHost(builder.Services);
app.UseHostFiles();
app.UseConfigureActions();
app.MarsUseTemplator();
app.Services.UseNodeWorkspace()
            .UseDatasourceWorkspace()
            .UseAppFrontMain();

var optionsFormsLocator = app.Services.GetRequiredService<OptionsFormsLocator>();
optionsFormsLocator.RegisterAssembly(typeof(ApiOptionEditForm).Assembly);

app.UsePlugins();
app.UseDevAdmin();
app.UseMarsNodes() //TODO: запросы на ресурсы тоже ловит AppFront.styles.css appsettings.json, если разрешить Match
   .UseMarsWebAppNodes();
app.UseDatasourceHost();
app.UseMarsWebSiteProcessor();
app.UseMarsExcel();
app.UseIfFeatureEnabled(FeatureFlags.DockerAgent, app => app.UseMarsDocker());
app.UseIfFeatureEnabled(FeatureFlags.AITool, app => app.UseMarsSemanticKernel());
app.UseIfFeatureEnabled(FeatureFlags.SingleSignOn, app => app.ApplicationServices.UseMarsSSO().UseMarsOAuthHost());

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
