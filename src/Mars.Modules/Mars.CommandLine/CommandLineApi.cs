using System.CommandLine;
using System.Reflection;
using Mars.CommandLine.Commands;
using Mars.CommandLine.Shared;
using Mars.Core.Extensions;
using Mars.Core.Models;
using Microsoft.AspNetCore.Builder;

namespace Mars.CommandLine;

//https://learn.microsoft.com/en-us/dotnet/standard/commandline/get-started-tutorial
public class CommandLineApi : ICommandLineApi
{
    public readonly RootCommand rootCommand;

    WebApplication _app = default!;
    public WebApplication app => _app;

    public bool IsContinueRun = false;

    private readonly List<Type> _modules = [];
    private readonly Dictionary<Type, CommandCli> cli = [];
    private bool _commandCliTypesLoaded = false;
    private bool _baseCommandCliTypesLoaded = false;

    //Type[] initalCommands = [typeof(InfoCommand)];

    internal static readonly string[] AllowedBaseCommands = ["info", "migrate"];
    private readonly Option _versionOption;
    private readonly Option _helpOption;
    private readonly Assembly _mainProgramAssembly;
    private readonly Type[] _initalCommands;

    /// <summary>Удалённые команды (тонкий клиент + исполнение пришедших по UDS) — см. <see cref="CliRemoteCommands"/>.</summary>
    public CliRemoteCommands Remote { get; }

    internal Option HelpOption => _helpOption;
    internal Option VersionOption => _versionOption;

    public CommandLineApi(Assembly mainProgramAssembly, Type[] initalCommands)
    {
        rootCommand = new RootCommand("Mars command line interface");

        var optionAppCfg = new Option<string>("-cfg", ["--config"]) { Description = "config .json file" };
        rootCommand.Add(optionAppCfg);

        var disableLogsOption = new Option<bool>("--disable-logs") { Description = "disable logging to /logs/app_{date}.log file" };
        rootCommand.Add(disableLogsOption);

        var localOption = new Option<bool>("--local") { Description = "run the command in-process even if a Mars server is already running" };
        rootCommand.Add(localOption);

        var noUdsOption = new Option<bool>("--no-uds") { Description = "start without the CLI unix domain socket (allows a second instance for the same directory)" };
        rootCommand.Add(noUdsOption);

        InitializeCliTypes(initalCommands);

        var builtInHelpOption = rootCommand.Options.First(s => s.Name == "--help");
        _helpOption = builtInHelpOption;

        var buildInVersionOption = rootCommand.Options.First(s => s.Name == "--version");
        buildInVersionOption.Aliases.Add("-v");
        _versionOption = buildInVersionOption;

        rootCommand.SetAction((parseResult) =>
        {
            IsContinueRun = true;
        });

        Remote = new CliRemoteCommands(this);
        _mainProgramAssembly = mainProgramAssembly;
        _initalCommands = initalCommands;
    }

    public void Setup(WebApplication app)
    {
        _app = app;
    }

    public void Register<TCommandCli>() where TCommandCli : CommandCli
        => _modules.Add(typeof(TCommandCli));

    void LoadBaseCommandCliTypes()
    {
        if (_baseCommandCliTypesLoaded) return;
        var cliTypes = GetEnumerableOfType<CommandCli>(_mainProgramAssembly).Except(_initalCommands);
        InitializeCliTypes([.. cliTypes, typeof(StatusCommandCli)]);
        _baseCommandCliTypesLoaded = true;
    }

    void LoadCommandCliTypes()
    {
        if (_commandCliTypesLoaded) return;
        InitializeCliTypes(_modules);
        _commandCliTypesLoaded = true;
    }

    internal void EnsureBaseCommandTypesLoaded() => LoadBaseCommandCliTypes();
    internal void EnsureCommandTypesLoaded() => LoadCommandCliTypes();

    void InitializeCliTypes(IEnumerable<Type> cliTypes)
    {
        foreach (var type in cliTypes)
        {
            var ctors = type.GetConstructors();
            var instance = ctors[0].Invoke([this]) as CommandCli;
            cli.Add(type, instance!);
        }
    }

    static IEnumerable<Type> GetEnumerableOfType<T>(Assembly assembly, params object[] constructorArgs) where T : class
    {
        List<Type> objects =
        [
            .. assembly.GetTypes()
            .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(T))),
        ];
        return objects;
    }

    public async Task<(bool invoked, bool isHelpCmd)> InvokeBaseCommands(string[] args)
    {
        LoadBaseCommandCliTypes();
        var parseResult = rootCommand.Parse(args);

        if (parseResult.Action == _helpOption.Action) return (invoked: false, isHelpCmd: true);

        if (parseResult.Action == _versionOption.Action || AllowedBaseCommands.Contains(parseResult.CommandResult.Command.Name))
        {
            await parseResult.InvokeAsync();
            return (invoked: true, isHelpCmd: false);
        }

        return (false, false);
    }

    public async Task InvokeCommands(string[] args)
    {
        LoadCommandCliTypes();

        await rootCommand.Parse(args).InvokeAsync();
    }

    public T GetCommand<T>() where T : CommandCli
    {
        return (cli[typeof(T)] as T)!;
    }

    public void OutResult(IUserActionResult result)
    {
        if (result.Ok)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(result.Message);
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(result.Message);
            Console.ResetColor();
        }
    }

    public bool Confirm(string message = "do you confirm your action?")
    {
        if (Remote.InRemoteInvocation)
        {
            throw new InvalidOperationException(
                "interactive confirmation is not available for remote CLI execution (the command runs inside the server process)");
        }

        bool confirmed;
        Console.WriteLine($"{message}");

        ConsoleKey response;
        do
        {
            Console.Write("press key - Yes (y) / No (n)  ");
            response = Console.ReadKey(false).Key;   // true is intercept key (dont show), false is show
            if (response != ConsoleKey.Enter)
                Console.WriteLine();

        } while (response != ConsoleKey.Y && response != ConsoleKey.N);

        confirmed = response == ConsoleKey.Y;

        return confirmed;
    }

    public T CheckGlobalOption<T>(string optionName, string[] args)
    {
        var parsed = rootCommand.Parse(args);
        var value = parsed.GetValue<T>(optionName);
        return (T)value!;
    }

    public void AddCommand(Command command)
    {
        rootCommand.Add(command);
    }
}
