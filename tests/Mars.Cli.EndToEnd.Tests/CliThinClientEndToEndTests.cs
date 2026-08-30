using System.Diagnostics;
using System.Text;
using Mars.CommandLine.Remote;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Mars.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end тонкого клиента: реальный процесс Mars.dll против stub-сервера на сокете.
/// Запускальные тесты вынесены в отдельный проект: Mars.Server.Tests — для кодовых тестов, не для запуска процессов.
/// Требует собранный Mars.WebApp (Debug), иначе тест пропускается.
/// </summary>
public class CliThinClientEndToEndTests
{
    [Fact]
    public async Task ModuleCommand_ServerRunning_ForwardsAndStreamsResult()
    {
        if (!MarsCliSocket.SupportsUnixDomainSockets) return; // OS без AF_UNIX
        var marsDll = FindMarsDll();
        if (marsDll is null) return; // Mars.WebApp не собран

        var workDir = NewWorkDir();
        var socketPath = MarsCliSocket.GetSocketPath(workDir, ["node", "list"]);

        await using var server = await StartStubServerAsync(socketPath,
            async (args, output, _, _) =>
            {
                await output.WriteLineAsync($"thin-client-ok args={string.Join(' ', args)}");
                return 7;
            });

        var (exitCode, stdout, stderr) = await RunMarsAsync(marsDll, workDir, ["node", "list"]);
        var dump = $"exit={exitCode}\nstdout:\n{stdout}\nstderr:\n{stderr}";

        Assert.True(exitCode == 7, dump);
        Assert.True(stdout.Contains("thin-client-ok args=node list"), dump);
        Assert.True(stdout.Contains("mars cli → remote exec: server is running (pid "), dump);
        Assert.True(!stdout.Contains(">RUN"), dump); // до запуска сервера не дошло
        // при живом сервере файловый лог пропускается (файл держит сервер): каталог не создаётся
        Assert.True(!Directory.Exists(Path.Combine(workDir, "data", "logs")), dump);
    }

    [Fact]
    public async Task ModuleCommandWithLocal_FallsBackToInProcess()
    {
        if (!MarsCliSocket.SupportsUnixDomainSockets) return; // OS без AF_UNIX
        var marsDll = FindMarsDll();
        if (marsDll is null) return; // Mars.WebApp не собран

        var workDir = NewWorkDir();
        var socketPath = MarsCliSocket.GetSocketPath(workDir, ["node", "list", "--local"]);

        await using var server = await StartStubServerAsync(socketPath,
            async (_, output, _, _) =>
            {
                await output.WriteLineAsync("thin-client-ok");
                return 7;
            });

        // --local: форвардинга нет даже при живом сервере — команда исполняется in-process
        var (exitCode, stdout, stderr) = await RunMarsAsync(marsDll, workDir, ["node", "list", "--local"]);
        var dump = $"exit={exitCode}\nstdout:\n{stdout}\nstderr:\n{stderr}";

        Assert.True(!stdout.Contains("thin-client-ok"), dump);
        Assert.True(!stdout.Contains("mars cli → remote exec"), dump);
    }

    [Fact]
    public async Task HelpFlag_ServerRunning_NotForwarded_PrintedByNormalFlow()
    {
        if (!MarsCliSocket.SupportsUnixDomainSockets) return; // OS без AF_UNIX
        var marsDll = FindMarsDll();
        if (marsDll is null) return; // Mars.WebApp не собран

        var workDir = NewWorkDir();
        var socketPath = MarsCliSocket.GetSocketPath(workDir, ["--help"]);

        await using var server = await StartStubServerAsync(socketPath,
            async (args, output, _, _) =>
            {
                await output.WriteLineAsync($"thin-client-ok args={string.Join(' ', args)}");
                return 0;
            });

        // -h тонким клиентом не обрабатывается: без форвардинга процесс проходит обычный
        // запуск, справку печатает System.CommandLine в InvokeCommands
        var (exitCode, stdout, stderr) = await RunMarsAsync(marsDll, workDir, ["--help"]);
        var dump = $"exit={exitCode}\nstdout:\n{stdout}\nstderr:\n{stderr}";

        Assert.True(!stdout.Contains("thin-client-ok"), dump); // не форвардилось
        Assert.True(!stdout.Contains("mars cli → remote exec"), dump);
        Assert.True(stdout.Contains("--help"), dump); // справка напечатана обычным потоком
        Assert.True(!stdout.Contains(">RUN"), dump); // до запуска сервера не дошло
        Assert.True(exitCode == 0, dump);
    }

    [Fact]
    public async Task HelpFlag_NoServer_PrintedByNormalFlow()
    {
        if (!MarsCliSocket.SupportsUnixDomainSockets) return; // OS без AF_UNIX
        var marsDll = FindMarsDll();
        if (marsDll is null) return; // Mars.WebApp не собран

        var workDir = NewWorkDir();
        // намеренно без stub-сервера: -h проходит обычный запуск, справка печатается в InvokeCommands

        var (exitCode, stdout, stderr) = await RunMarsAsync(marsDll, workDir, ["-h"]);
        var dump = $"exit={exitCode}\nstdout:\n{stdout}\nstderr:\n{stderr}";

        Assert.True(exitCode == 0, dump);
        Assert.True(stdout.Contains("--help"), dump); // текст справки напечатан
        Assert.True(!stdout.Contains("mars cli → remote exec"), dump); // не форвардилось
        Assert.True(!stdout.Contains(">RUN"), dump); // до запуска сервера не дошло
    }

    [Fact]
    public async Task Status_ServerRunning_ShowsServerInfo()
    {
        if (!MarsCliSocket.SupportsUnixDomainSockets) return; // OS без AF_UNIX
        var marsDll = FindMarsDll();
        if (marsDll is null) return; // Mars.WebApp не собран

        var workDir = NewWorkDir();
        var socketPath = MarsCliSocket.GetSocketPath(workDir, ["status"]);

        await using var server = await StartStubServerAsync(socketPath,
            async (_, output, _, _) =>
            {
                await output.WriteLineAsync("unused");
                return 0;
            });

        // status отвечает сам probe сокета — до запуска сервера не доходит
        var (exitCode, stdout, stderr) = await RunMarsAsync(marsDll, workDir, ["status"]);
        var dump = $"exit={exitCode}\nstdout:\n{stdout}\nstderr:\n{stderr}";

        Assert.True(exitCode == 0, dump);
        Assert.True(stdout.Contains("mars cli: running"), dump);
        Assert.True(stdout.Contains("uptime"), dump);
        Assert.True(stdout.Contains(socketPath), dump);
        Assert.True(!stdout.Contains(">RUN"), dump); // до запуска сервера не дошло
    }

    [Fact]
    public async Task Status_NoServer_ReportsNotRunning()
    {
        if (!MarsCliSocket.SupportsUnixDomainSockets) return; // OS без AF_UNIX
        var marsDll = FindMarsDll();
        if (marsDll is null) return; // Mars.WebApp не собран

        var workDir = NewWorkDir();

        var (exitCode, stdout, stderr) = await RunMarsAsync(marsDll, workDir, ["status"]);
        var dump = $"exit={exitCode}\nstdout:\n{stdout}\nstderr:\n{stderr}";

        Assert.True(exitCode == 1, dump);
        Assert.True(stdout.Contains("mars cli: not running"), dump);
        Assert.True(!stdout.Contains(">RUN"), dump); // до запуска сервера не дошло
    }

    static string NewWorkDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"mars-cli-e2e-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);

        // базовый appsettings.json из сборки WebApp (секции OpenTelemetry и пр.),
        // иначе ConfigureBuilder падает на отсутствующих настройках;
        // appsettings.Local.json сверху — InMemory-провайдер, чтобы собрать приложение без реальной БД;
        // AutoMigrate отключён: InMemory-провайдер не поддерживает миграции
        var marsDll = FindMarsDll();
        if (marsDll is not null)
        {
            var appDir = Path.GetDirectoryName(marsDll)!;
            var appsettings = Path.Combine(appDir, "appsettings.json");
            if (File.Exists(appsettings))
                File.Copy(appsettings, Path.Combine(dir, "appsettings.json"));

            // шаблоны фронтов (admin/default/landing): fall-through-команды (-h, --local)
            // проходят ConfigureApp, где EnsureDefaultFront/UseFront требуют Res/front_templates
            var resDir = Path.Combine(appDir, "Res");
            if (Directory.Exists(resDir))
                CopyDirectory(resDir, Path.Combine(dir, "Res"));
        }

        File.WriteAllText(Path.Combine(dir, "appsettings.Local.json"), """
        {
          "ConnectionStrings": {
            "DefaultConnection": "InMemoryDb"
          },
          "AppDatabaseMigrationOptions": {
            "AutoMigrate": false
          }
        }
        """);
        return dir;
    }

    static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        foreach (var subDir in Directory.GetDirectories(source))
            CopyDirectory(subDir, Path.Combine(destination, Path.GetFileName(subDir)));
    }

    static async Task<(int ExitCode, string Stdout, string Stderr)> RunMarsAsync(string marsDll, string workDir, string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            // дочерний процесс ставит Console.OutputEncoding = UTF8
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        psi.ArgumentList.Add(marsDll);
        foreach (var arg in args) psi.ArgumentList.Add(arg);
        psi.Environment["MARS_SETUP_WIZARD"] = "0";
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        // отключает FixDebugModeBaseDirectory: в Debug-сборке вне VS процесс перенёс бы
        // CWD в src/Mars.WebApp, и сокет искался бы не в workDir
        psi.Environment["VisualStudioEdition"] = "test";

        using var process = Process.Start(psi)!;
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token);
        return (process.ExitCode, await stdoutTask, await stderrTask);
    }

    static async Task<IAsyncDisposable> StartStubServerAsync(string socketPath, Func<string[], TextWriter, TextWriter, CancellationToken, Task<int>> executor)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.WebHost.ConfigureKestrel(options => options.ConfigureCliSocket(builder.Configuration, socketPath, out _));

        var app = builder.Build();
        app.MapCliSocketEndpoints(new CliServerInfo
        {
            ProtocolVersion = MarsCliSocket.ProtocolVersion,
            Pid = Environment.ProcessId,
            Version = "stub",
            StartedAt = DateTimeOffset.Now,
            SocketPath = socketPath,
        }, executor);

        await app.StartAsync();

        return new StubServer(app, socketPath);
    }

    sealed class StubServer(WebApplication app, string socketPath) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await app.StopAsync();
            await app.DisposeAsync();
            try
            {
                File.Delete(socketPath);
            }
            catch
            {
                // best effort
            }
        }
    }

    /// <summary>tests/Mars.Cli.EndToEnd.Tests/bin/&lt;cfg&gt;/net10.0 → src/Mars.WebApp/bin/&lt;cfg&gt;/net10.0/Mars.dll</summary>
    static string? FindMarsDll()
    {
        // TrimEnd обязателен: с завершающим разделителем GetDirectoryName вернёт net10.0 вместо конфигурации
        var baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        var cfg = Path.GetFileName(Path.GetDirectoryName(baseDir)!);
        var repoRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        var candidate = Path.Combine(repoRoot, "src", "Mars.WebApp", "bin", cfg, "net10.0", "Mars.dll");
        return File.Exists(candidate) ? candidate : null;
    }
}
