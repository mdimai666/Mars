using System.CommandLine;
using Mars.CommandLine.Abstractions;
using Mars.CommandLine.Remote;

namespace Mars.CommandLine.Commands;

/// <summary>
/// `Mars.exe status` — запущен ли сервер для этой директории.
/// Основной путь — fast-path в Program.cs (до сборки приложения); in-process
/// исполнение достижимо только когда UDS выключен (IsTesting/нет поддержки ОС).
/// </summary>
public class StatusCommandCli : CommandCli
{
    public const string CommandName = "status";

    public StatusCommandCli(CommandLineApi cli) : base(cli)
    {
        var statusCommand = new Command(CommandName, "show whether the Mars server is running for this directory (exit code 0 = running, 1 = not running)");
        statusCommand.SetAction((p, ct) => MarsCliStatus.PrintAsync());
        cli.AddCommand(statusCommand);
    }
}
