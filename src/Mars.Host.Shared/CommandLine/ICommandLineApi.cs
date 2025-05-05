using System.CommandLine;
using Mars.Core.Models;
using Microsoft.AspNetCore.Builder;

namespace Mars.Host.Shared.CommandLine;

public interface ICommandLineApi
{
    WebApplication app { get; }
    void OutResult(IUserActionResult result);
    bool Confirm(string message = "do you confirm your action?");
    void AddCommand(Command command);
    public T GetCommand<T>() where T : CommandCli;

    static List<Type> modulesCommandList = new();

    public static void Register<TCommandCli>() => modulesCommandList.Add(typeof(TCommandCli));
    public static IReadOnlyCollection<Type> GetModulesCommands => modulesCommandList;
}
