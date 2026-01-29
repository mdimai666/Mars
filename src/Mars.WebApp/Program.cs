using System.Diagnostics;
using System.Text;
using Mars;
using Mars.CommandLine;
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

var builder = WebApplication.CreateBuilder(args);
MarsWebAppStartup.ConfigureBuilder(builder, args);

var app = builder.Build();

var commandsApi = new CommandLineApi();
commandsApi.Setup(app);
var (baseCmdInvoked, isHelpCmd) = await commandsApi.InvokeBaseCommands(IsTesting ? [] : args);
if (baseCmdInvoked) return;

Console.WriteLine(Mars.Core.Extensions.MarsStringExtensions.HelloText());

if (!isHelpCmd) commandsApi.GetCommand<InfoCommand>().ShowInfoCommand(showHello: false);

await MarsWebAppStartup.ConfigureApp(app, builder, args);

await commandsApi.InvokeCommands(IsTesting ? [] : args);
if (!commandsApi.IsContinueRun) return;

startWatch.Stop();
Console.WriteLine($"start in : {startWatch.ElapsedMilliseconds.ToString("0")}ms");
Console.WriteLine(">RUN");

app.Run();
