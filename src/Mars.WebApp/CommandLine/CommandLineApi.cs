using System.CommandLine;
using System.Reflection;
using Mars.Core.Extensions;
using Mars.Core.Models;
using Mars.Host.Shared.CommandLine;

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

    Type[] initalCommands = [typeof(InfoCommand)];

    static readonly string[] _allowedBaseCommands = ["info", "migrate"];
    private readonly Option _versionOption;
    private readonly Option _helpOption;

    public CommandLineApi()
    {
        rootCommand = new RootCommand("Mars command line interface");

        var optionAppCfg = new Option<string>("-cfg", ["--config"]) { Description = "config .json file" };
        rootCommand.Add(optionAppCfg);

        var disableLogsOption = new Option<bool>("--disable-logs") { Description = "disable logging to /logs/app_{date}.log file" };
        rootCommand.Add(disableLogsOption);

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
    }

    public void Setup(WebApplication app)
    {
        _app = app;
    }

    public void Register<TCommandCli>() where TCommandCli : CommandCli
        => _modules.Add(typeof(TCommandCli));

    void LoadBaseCommandCliTypes()
    {
        var cliTypes = GetEnumerableOfType<CommandCli>(typeof(Program).Assembly).Except(initalCommands);
        InitializeCliTypes(cliTypes);
    }

    void LoadCommandCliTypes()
    {
        InitializeCliTypes(_modules);
    }

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

        if (parseResult.Action == _versionOption.Action || _allowedBaseCommands.Contains(parseResult.CommandResult.Command.Name))
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
