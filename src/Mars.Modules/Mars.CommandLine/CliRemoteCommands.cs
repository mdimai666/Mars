using Mars.CommandLine.Commands;
using Mars.CommandLine.Remote;

namespace Mars.CommandLine;

/// <summary>
/// Удалённые команды Mars CLI — обе стороны канала unix domain socket.
/// Клиентская (тонкий клиент): классификация, probe и форвардинг команды живому инстансу —
/// <see cref="InvokeAsync"/>, вызывается из Program.cs после <see cref="CommandLineApi.InvokeBaseCommands"/>.
/// Серверная: исполнение пришедшей по сокету команды в рантайме этого сервера —
/// <see cref="InvokeRemoteAsync"/>, подключается CliSocketServer.
/// </summary>
public class CliRemoteCommands
{
    readonly CommandLineApi _api;

    // удалённое исполнение: Console перехвачен фрейм-райтерами, интерактив недоступен
    bool _inRemoteInvocation;
    readonly SemaphoreSlim _remoteInvocationLock = new(1, 1);

    public bool InRemoteInvocation => _inRemoteInvocation;

    public CliRemoteCommands(CommandLineApi api)
    {
        _api = api;
    }

    /// <summary>
    /// Тонкий клиент: если Mars уже запущен для этой директории — команда исполняется в его рантайме
    /// через unix domain socket, второй инстанс приложения (БД, плагины) не поднимается.
    /// status отвечает probe'ом сокета; -h/--help/-v/--version форвардятся живому серверу
    /// (у него полное дерево команд), при его отсутствии печатаются локально;
    /// команды модулей только форвардятся живому серверу, иначе исполняются in-process.
    /// </summary>
    /// <returns>(invoked, exitCode): invoked=true — команда обработана и процесс должен завершиться с exitCode.</returns>
    public async Task<(bool invoked, int exitCode)> InvokeAsync(string[] args)
    {
        if (!MarsCliSocket.SupportsUnixDomainSockets) return (false, 0);

        var invocationKind = ClassifyInvocation(args);

        if (invocationKind == CliInvocationKind.StatusQuery)
        {
            var exitCode = await MarsCliStatus.PrintAsync(args);
            return (true, exitCode);
        }

        if (invocationKind == CliInvocationKind.HelpOrVersion)
        {
            return (false, 0);
        }

        if (invocationKind == CliInvocationKind.RemoteCandidate
            && !_api.CheckGlobalOption<bool>("--local", args))
        {
            var forwarded = await TryExecOnRunningServerAsync(args);
            if (forwarded is not null) return (true, forwarded.Value);
        }

        return (false, 0);
    }

    /// <summary>
    /// Форвардит команду живому инстансу. null — сервер не запущен или несовместимый протокол
    /// (тогда вызывающий переходит к in-process исполнению).
    /// </summary>
    async Task<int?> TryExecOnRunningServerAsync(string[] forwardArgs)
    {
        var cliSocketPath = MarsCliSocket.GetSocketPath(forwardArgs);
        var runningServer = await MarsCliSocket.ProbeAsync(cliSocketPath);
        if (runningServer is null) return null;
        if (runningServer.ProtocolVersion != MarsCliSocket.ProtocolVersion)
        {
            Console.WriteLine(
                $"mars cli: running server (pid {runningServer.Pid}) speaks protocol v{runningServer.ProtocolVersion}, " +
                $"this CLI supports v{MarsCliSocket.ProtocolVersion} — falling back to in-process run");
            return null;
        }

        // заголовок тонкого клиента вместо логотипа и стартовых логов:
        // команда исполняется в живом инстансе, второй процесс не поднимается
        var serverVersion = runningServer.Version.Split('+')[0];
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"mars cli → remote exec: server is running (pid {runningServer.Pid}, v{serverVersion})");
        Console.WriteLine($"executing in the live instance: {string.Join(' ', forwardArgs)}");
        Console.ResetColor();
        Console.WriteLine();

        return await CliRemoteClient.ExecAsync(cliSocketPath, forwardArgs);
    }

    /// <summary>
    /// Классификация вызова для тонкого клиента. Работает парсером, как InvokeBaseCommands:
    /// help/version определяются по Action, базовые команды — по имени команды.
    /// RunServer — только когда распознан корень (команды нет); любая совпавшая реальная
    /// команда (option/user/role/node/…) — кандидат на форвардинг живому инстансу.
    /// </summary>
    public CliInvocationKind ClassifyInvocation(string[] args)
    {
        _api.EnsureBaseCommandTypesLoaded();
        var parseResult = _api.rootCommand.Parse(args);

        if (parseResult.Action == _api.HelpOption.Action || parseResult.Action == _api.VersionOption.Action)
            return CliInvocationKind.HelpOrVersion;

        var matchedCommand = parseResult.CommandResult.Command;
        if (matchedCommand.Name == StatusCommandCli.CommandName)
            return CliInvocationKind.StatusQuery;

        if (CommandLineApi.AllowedBaseCommands.Contains(matchedCommand.Name))
            return CliInvocationKind.BaseCommand;

        if (matchedCommand == _api.rootCommand)
        {
            // команды нет: чистый запуск сервера, либо неизвестный токен (команда модуля)
            return parseResult.Errors.Count == 0
                ? CliInvocationKind.RunServer
                : CliInvocationKind.RemoteCandidate;
        }

        return CliInvocationKind.RemoteCandidate;
    }

    /// <summary>
    /// Исполнение команды, пришедшей от CLI-клиента поверх UDS, в рантайме этого сервера.
    /// Вывод перехватывается фрейм-райтерами ответа двумя путями: InvocationConfiguration.Output/Error
    /// (пишет сам System.CommandLine — help, ошибки парсинга) и перехватом Console
    /// (команды пишут в Console напрямую). Исполнения сериализуются, потому что Console у процесса один.
    /// </summary>
    public async Task<int> InvokeRemoteAsync(string[] args, TextWriter output, TextWriter error, CancellationToken cancellationToken)
    {
        await _remoteInvocationLock.WaitAsync(cancellationToken);

        var oldOut = Console.Out;
        var oldError = Console.Error;
        _inRemoteInvocation = true;
        try
        {
            Console.SetOut(output);
            Console.SetError(error);

            _api.EnsureCommandTypesLoaded();
            var parseResult = _api.rootCommand.Parse(args);
            parseResult.InvocationConfiguration.Output = output;
            parseResult.InvocationConfiguration.Error = error;
            return await parseResult.InvokeAsync(parseResult.InvocationConfiguration, cancellationToken);
        }
        finally
        {
            Console.SetOut(oldOut);
            Console.SetError(oldError);
            _inRemoteInvocation = false;
            _remoteInvocationLock.Release();
        }
    }
}

public enum CliInvocationKind
{
    /// <summary>Команды нет — обычный запуск сервера.</summary>
    RunServer,
    /// <summary>Базовые команды (info/migrate) — всегда исполняются in-process.</summary>
    BaseCommand,
    /// <summary>Только -h/--help/-v/--version: форвардится живому серверу (полное дерево команд), иначе in-process.</summary>
    HelpOrVersion,
    /// <summary>Команда status: probe сокета, приложение не собирается ни при каком исходе.</summary>
    StatusQuery,
    /// <summary>Команда модуля — кандидат на передачу в запущенный инстанс.</summary>
    RemoteCandidate,
}
