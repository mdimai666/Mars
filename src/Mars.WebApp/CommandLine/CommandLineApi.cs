using System.CommandLine;
using System.Reflection;
using Mars.Host.Shared.CommandLine;
using Mars.Core.Models;

namespace Mars.CommandLine;

//https://learn.microsoft.com/en-us/dotnet/standard/commandline/get-started-tutorial
public class CommandLineApi : ICommandLineApi
{
    public readonly RootCommand rootCommand;

    WebApplication _app = default!;
    public WebApplication app => _app;

    public bool IsContinueRun = false;

    Dictionary<Type, CommandCli> cli = new();

    Type[] initalCommands = [typeof(MainCommand)];

    public CommandLineApi()
    {
        rootCommand = new RootCommand("Mars command line interface");

        var optionAppCfg = new Option<string>("-cfg", "config .json file");
        rootCommand.AddGlobalOption(optionAppCfg);

        var disableLogsOption = new Option<bool>("--disable-logs", "disable logging to /logs/app_{date}.log file");
        rootCommand.AddGlobalOption(disableLogsOption);

        InitializeCliTypes(initalCommands);

        rootCommand.SetHandler(() =>
        {
            IsContinueRun = true;
        });
    }

    public void Setup(WebApplication app)
    {
        _app = app;
    }

    void LoadBaseCommandCliTypes()
    {
        var cliTypes = GetEnumerableOfType<CommandCli>(typeof(Program).Assembly).Except(initalCommands);
        InitializeCliTypes(cliTypes);
    }

    void LoadCommandCliTypes()
    {
        InitializeCliTypes(ICommandLineApi.GetModulesCommands);
    }

    void InitializeCliTypes(IEnumerable<Type> cliTypes)
    {
        foreach (var type in cliTypes)
        {
            var ctors = type.GetConstructors();
            var instance = ctors[0].Invoke([this]) as CommandCli;
            cli.Add(type, instance);
        }
    }

    static IEnumerable<Type> GetEnumerableOfType<T>(Assembly assembly, params object[] constructorArgs) where T : class
    {
        List<Type> objects = new List<Type>();
        foreach (Type type in
            assembly.GetTypes()
            .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(T))))
        {
            objects.Add(type);
        }
        return objects;
    }

    public async Task InvokeBaseCommands(string[] args)
    {
        LoadBaseCommandCliTypes();
        await rootCommand.InvokeAsync(args);
    }

    public async Task InvokeCommands(string[] args)
    {
        LoadCommandCliTypes();
        await rootCommand.InvokeAsync(args);
    }

    public T GetCommand<T>() where T : CommandCli
    {
        return cli[typeof(T)] as T;
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
        var option = rootCommand.Options.FirstOrDefault(option => option.Aliases.Contains(optionName))
            ?? throw new Exception($"option '{optionName} not found'");
        var value = parsed.GetValueForOption(option);

        return (T)value;
    }

    public void AddCommand(Command command)
    {
        rootCommand.Add(command);
    }
}
