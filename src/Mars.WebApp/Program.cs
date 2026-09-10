using System.Diagnostics;
using System.Text;
using Mars;
using Mars.CommandLine;
using Mars.CommandLine.Abstractions;
using Mars.CommandLine.Remote;
using Mars.Setup;
using Mars.UseStartup;
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
FixDebugModeBaseDirectory.SetBaseDirectory();
#endif

var builder = WebApplication.CreateBuilder(args);

// Setup wizard — runs BEFORE the main application when no DB configuration exists
if (SetupWizardHost.ShouldRunWizard())
{
    await SetupWizardHost.RunAsync(args);
    builder.Configuration.AddJsonFile(SetupWizardHost.WizardConfigPath, optional: false, reloadOnChange: true);
}

if (!IsTesting) await MarsCliSocket.DetectRunningServerAsync(args);

MarsWebAppStartup.ConfigureBuilder(builder, args);

var app = builder.Build();

var commandsApi = app.Services.GetRequiredService<ICommandLineApi>() as CommandLineApi;
commandsApi.Setup(app);
var (baseCmdInvoked, isHelpCmd) = await commandsApi.InvokeBaseCommands(IsTesting ? [] : args);
if (baseCmdInvoked) return 0;

var (remoteCmdInvoked, remoteExitCode) = await commandsApi.Remote.InvokeAsync(IsTesting ? [] : args);
if (remoteCmdInvoked) return remoteExitCode;

Console.WriteLine(Mars.Core.Extensions.MarsStringExtensions.HelloText());

if (!isHelpCmd) commandsApi.GetCommand<InfoCommand>().ShowInfoCommand(showHello: false);

await MarsWebAppStartup.ConfigureApp(app, builder, args);

await commandsApi.InvokeCommands(IsTesting ? [] : args);
if (!commandsApi.IsContinueRun) return 0;

startWatch.Stop();
Console.WriteLine($"start in : {startWatch.ElapsedMilliseconds.ToString("0")}ms");
Console.WriteLine(">RUN");

return app.RunSafelyMessageWrapper();
