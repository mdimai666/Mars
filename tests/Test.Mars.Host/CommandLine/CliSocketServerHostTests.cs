using System.Net.Http.Json;
using System.Text.Json;
using Mars.CommandLine.Remote;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Test.Mars.Host.CommandLine;

/// <summary>
/// Hermetic-прогон серверной стороны CLI-сокета: минимальный WebApplication
/// с тем же ConfigureCliSocket/MapCliSocketEndpoints, что и в Mars.WebApp.
/// </summary>
public class CliSocketServerHostTests
{
    [Fact]
    public async Task PingAndExec_OverUds_Work()
    {
        if (!MarsCliSocket.SupportsUnixDomainSockets) return; // OS без AF_UNIX

        await using var server = await TestCliServer.StartAsync(StubExecutorAsync);

        // ping
        var info = await MarsCliSocket.ProbeAsync(server.SocketPath);
        Assert.NotNull(info);
        Assert.Equal(MarsCliSocket.ProtocolVersion, info!.ProtocolVersion);
        Assert.Equal(Environment.ProcessId, info.Pid);
        Assert.Equal(server.SocketPath, info.SocketPath);

        // exec: NDJSON-кадры stdout/stderr/exit
        using var http = MarsCliSocket.CreateHttpClient(server.SocketPath, TimeSpan.FromSeconds(10));
        using var response = await http.PostAsJsonAsync(MarsCliSocket.ExecPath, new CliExecRequest
        {
            ProtocolVersion = MarsCliSocket.ProtocolVersion,
            Args = ["abc", "def"],
        });
        Assert.True(response.IsSuccessStatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var frames = body.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                         .Select(line => JsonSerializer.Deserialize<CliFrame>(line))
                         .ToArray();

        Assert.Contains(frames, f => f!.Type == CliFrame.Out && f.Data!.Contains("hello abc def"));
        Assert.Contains(frames, f => f!.Type == CliFrame.Error && f.Data!.Contains("oops"));
        var exit = frames.Last();
        Assert.Equal(CliFrame.Exit, exit!.Type);
        Assert.Equal(7, exit.Code);
    }

    [Fact]
    public async Task CliEndpoints_OverTcp_Return404()
    {
        if (!MarsCliSocket.SupportsUnixDomainSockets) return; // OS без AF_UNIX

        await using var server = await TestCliServer.StartAsync(StubExecutorAsync);
        using var http = new HttpClient { BaseAddress = new Uri(server.TcpUrl) };

        var ping = await http.GetAsync(MarsCliSocket.PingPath);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, ping.StatusCode);

        var exec = await http.PostAsJsonAsync(MarsCliSocket.ExecPath, new CliExecRequest
        {
            ProtocolVersion = MarsCliSocket.ProtocolVersion,
            Args = ["node", "list"],
        });
        Assert.Equal(System.Net.HttpStatusCode.NotFound, exec.StatusCode);
    }

    [Fact]
    public async Task Exec_WrongProtocolVersion_Returns400()
    {
        if (!MarsCliSocket.SupportsUnixDomainSockets) return; // OS без AF_UNIX

        await using var server = await TestCliServer.StartAsync(StubExecutorAsync);
        using var http = MarsCliSocket.CreateHttpClient(server.SocketPath, TimeSpan.FromSeconds(10));

        using var response = await http.PostAsJsonAsync(MarsCliSocket.ExecPath, new CliExecRequest
        {
            ProtocolVersion = MarsCliSocket.ProtocolVersion + 100,
            Args = ["anything"],
        });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CliRemoteClient_StreamsOutputToConsole_AndReturnsExitCode()
    {
        if (!MarsCliSocket.SupportsUnixDomainSockets) return; // OS без AF_UNIX

        await using var server = await TestCliServer.StartAsync(StubExecutorAsync);

        var oldOut = Console.Out;
        var oldError = Console.Error;
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        try
        {
            Console.SetOut(stdout);
            Console.SetError(stderr);
            var exitCode = await CliRemoteClient.ExecAsync(server.SocketPath, ["xyz"]);

            Assert.Equal(7, exitCode);
            Assert.Contains("hello xyz", stdout.ToString());
            Assert.Contains("oops", stderr.ToString());
        }
        finally
        {
            Console.SetOut(oldOut);
            Console.SetError(oldError);
        }
    }

    [Fact]
    public async Task DetectRunningServer_LiveServer_SetsFlag_DeadSocket_ClearsIt()
    {
        if (!MarsCliSocket.SupportsUnixDomainSockets) return; // OS без AF_UNIX

        var server = await TestCliServer.StartAsync(StubExecutorAsync);
        await MarsCliSocket.DetectRunningServerAsync(server.SocketPath);
        Assert.NotNull(MarsCliSocket.RunningServer);
        Assert.Equal(Environment.ProcessId, MarsCliSocket.RunningServer!.Pid);

        await server.DisposeAsync();

        // stale-файл (сервер упал без unlink): файла касаться нельзя — connect отказывает
        await File.WriteAllTextAsync(server.SocketPath, "");
        try
        {
            await MarsCliSocket.DetectRunningServerAsync(server.SocketPath);
            Assert.Null(MarsCliSocket.RunningServer);
        }
        finally
        {
            File.Delete(server.SocketPath);
        }
    }

    static async Task<int> StubExecutorAsync(string[] args, TextWriter output, TextWriter error, CancellationToken ct)
    {
        await output.WriteLineAsync($"hello {string.Join(' ', args)}");
        await error.WriteLineAsync("oops");
        return 7;
    }
}

/// <summary>
/// Минимальный сервер с UDS-эндпоинтом и одним TCP-адресом (порт 0 — динамический).
/// </summary>
sealed file class TestCliServer : IAsyncDisposable
{
    public required WebApplication App { get; init; }
    public required string SocketPath { get; init; }
    public required string TcpUrl { get; init; }

    public static async Task<TestCliServer> StartAsync(Func<string[], TextWriter, TextWriter, CancellationToken, Task<int>> executor)
    {
        var socketPath = Path.Combine(Path.GetTempPath(), $"mars-test-{Guid.NewGuid():N}.sock");

        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0"); // порт 0 — Kestrel выберет свободный
        builder.WebHost.ConfigureKestrel(options => options.ConfigureCliSocket(builder.Configuration, socketPath, out _));

        var app = builder.Build();
        app.MapCliSocketEndpoints(new CliServerInfo
        {
            ProtocolVersion = MarsCliSocket.ProtocolVersion,
            Pid = Environment.ProcessId,
            Version = "test",
            StartedAt = DateTimeOffset.Now,
            SocketPath = socketPath,
        }, executor);

        await app.StartAsync();

        var addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses;
        var tcpUrl = addresses.First(a => a.StartsWith("http://") && !a.Contains("unix:"));

        return new TestCliServer { App = app, SocketPath = socketPath, TcpUrl = tcpUrl };
    }

    public async ValueTask DisposeAsync()
    {
        await App.StopAsync();
        await App.DisposeAsync();
        try
        {
            File.Delete(SocketPath);
        }
        catch
        {
            // best effort
        }
    }
}
