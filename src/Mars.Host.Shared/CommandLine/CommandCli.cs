using Mars.Core.Models;
using Microsoft.AspNetCore.Builder;

namespace Mars.Host.Shared.CommandLine;

public abstract class CommandCli
{
    protected readonly ICommandLineApi cli;
    public WebApplication app => cli.app;

    public CommandCli(ICommandLineApi cli)
    {
        this.cli = cli;
    }

    public void OutResult(IUserActionResult result)
    {
        cli.OutResult(result);
    }

    public bool Confirm(string message = "do you confirm your action?")
    {
        return cli.Confirm(message);
    }

}
