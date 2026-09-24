using System.CommandLine;
using Mars.Core.Models;
using Microsoft.AspNetCore.Builder;

namespace Mars.CommandLine.Abstractions;

public interface ICommandLineApi
{
    WebApplication app { get; }

    /// <summary>Команда исполняется в рантайме сервера по UDS от тонкого клиента
    /// (Console перехвачен, интерактив и stdin клиента недоступны).</summary>
    bool InRemoteInvocation { get; }

    void OutResult(IUserActionResult result);
    bool Confirm(string message = "do you confirm your action?");
    void AddCommand(Command command);
    public T GetCommand<T>() where T : CommandCli;

    public void Register<TCommandCli>() where TCommandCli : CommandCli;
}
