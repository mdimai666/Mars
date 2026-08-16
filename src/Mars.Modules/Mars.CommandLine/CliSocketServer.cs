using Mars.CommandLine.Remote;
using Mars.CommandLine.Shared;
using Mars.Host.Shared.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mars.CommandLine;

/// <summary>
/// Mars-обвязка CLI-сокета: скипы для тестов/--no-uds, обработка stale-файла,
/// Kestrel-биндинг, lifecycle (chmod/unlink) и маппинг /_cli-эндпоинтов.
///
/// Коллизия инстансов: если сокет уже слушается другим Mars, инстанс не отказывается
/// от сборки (базовые команды info/migrate и --local-команды должны работать и при
/// живом сервере) — упадёт bind при app.Run(), Program.cs превращает это в понятное сообщение.
/// </summary>
public static class CliSocketServer
{
    public const string SocketPathConfigKey = "Mars:CliSocketPath";

    public static void AddMarsCliSocket(this WebApplicationBuilder builder, CommandLineApi commandsApi, string[] args, IMarsStartupInfo marsStartupInfo)
    {
        if (marsStartupInfo.IsTesting) return;
        if (!MarsCliSocket.SupportsUnixDomainSockets)
        {
            Console.WriteLine("cli socket: Unix domain sockets are not supported by this OS — CLI runs in-process only");
            return;
        }
        if (commandsApi.CheckGlobalOption<bool>("--no-uds", args)) return;

        var socketPath = MarsCliSocket.GetSocketPath(args);

        var running = MarsCliSocket.ProbeAsync(socketPath).GetAwaiter().GetResult();
        if (running is not null)
        {
            Console.WriteLine(
                $"cli socket: another Mars instance is already listening (pid {running.Pid}, version {running.Version}, started {running.StartedAt:s}) — " +
                $"this instance will not start unless the other one is stopped or launched with --no-uds");
        }
        else if (File.Exists(socketPath))
        {
            File.Delete(socketPath); // stale-файл от процесса, умершего без unlink (kill -9, падение)
        }

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ConfigureCliSocket(builder.Configuration, socketPath, out var urlsPlan);
            foreach (var warning in urlsPlan.Warnings)
            {
                Console.WriteLine("cli socket: " + warning);
            }
        });

        // путь нужен на этапе ConfigureApp/Run — передаём через конфигурацию
        builder.Configuration.AddInMemoryCollection([KeyValuePair.Create<string, string?>(SocketPathConfigKey, socketPath)]);
    }

    public static void UseMarsCliSocket(this WebApplication app, IMarsStartupInfo marsStartupInfo)
    {
        var socketPath = app.Configuration[SocketPathConfigKey];
        if (socketPath is null) return;

        var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
        var serverInfo = new CliServerInfo
        {
            ProtocolVersion = MarsCliSocket.ProtocolVersion,
            Pid = Environment.ProcessId,
            Version = marsStartupInfo.Version,
            StartedAt = marsStartupInfo.StartDateTime,
            SocketPath = socketPath,
        };

        // bind() создаёт файл сокета с правами umask (обычно доступны всем);
        // ограничиваем доступ пользователем-владельцем сразу после старта слушателя
        lifetime.ApplicationStarted.Register(() =>
        {
            if (!OperatingSystem.IsLinux()) return;
            try
            {
                File.SetUnixFileMode(socketPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
            catch
            {
                // best effort
            }
        });

        lifetime.ApplicationStopped.Register(() =>
        {
            try
            {
                File.Delete(socketPath);
            }
            catch
            {
                // best effort
            }
        });

        var commandsApi = (CommandLineApi)app.Services.GetRequiredService<ICommandLineApi>();
        app.MapCliSocketEndpoints(serverInfo, (cliArgs, output, error, ct) => commandsApi.Remote.InvokeRemoteAsync(cliArgs, output, error, ct));

        Console.WriteLine($"cli socket: {socketPath}");
    }

    public static int RunSafelyMessageWrapper(this WebApplication app)
    {
        try
        {
            app.Run();
            return 0;
        }
        catch (AddressInUseException)
        {
            // типичная причина — уже живой инстанс Mars для этой директории (порт и CLI-сокет заняты)
            var cliSocketPath = app.Configuration[CliSocketServer.SocketPathConfigKey];
            var extra = cliSocketPath is null ? "" : $" Another Mars instance may be listening on {cliSocketPath}.";
            Console.Error.WriteLine($"Address already in use.{extra} Stop the other instance or start this one with --no-uds.");
            return 1;
        }
    }
}
