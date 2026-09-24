using Docker.DotNet;
using FluentAssertions;
using Mars.Docker.Abstractions.Dto;
using Mars.Docker.Host.Options;
using Mars.Docker.Host.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Mars.Docker.Tests;

/// <summary>
/// Интеграционные тесты против живого docker-демона; opt-in через MARS_DOCKER_TESTS=1.
/// </summary>
public class DockerServiceIntegrationTests : IDisposable
{
    private const string TestImage = "alpine:3.20";

    private readonly DockerClient _client = new DockerClientConfiguration().CreateClient();
    private readonly IDockerService _service;

    public DockerServiceIntegrationTests()
    {
        _service = new DockerService(_client,
            Options.Create(new DockerOptions { DefaultRunTimeoutSeconds = 120 }),
            new MemoryCache(new MemoryCacheOptions()));
    }

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }

    [DockerFact]
    public async Task EnsureImage_pulls_when_missing()
    {
        await _service.EnsureImage(TestImage, null, CancellationToken.None);

        var inspect = await _service.InspectImage(TestImage, CancellationToken.None);
        inspect.Should().NotBeNull();
    }

    [DockerFact]
    public async Task RunOnce_captures_stdout_and_exit_code()
    {
        var result = await _service.RunContainerOnce(new RunContainerQuery
        {
            Image = TestImage,
            Cmd = ["echo", "hello-mars"],
        }, CancellationToken.None);

        result.TimedOut.Should().BeFalse();
        result.ExitCode.Should().Be(0);
        result.Stdout.Trim().Should().Be("hello-mars");
    }

    [DockerFact]
    public async Task RunOnce_passes_stdin_and_captures_stderr()
    {
        if (OperatingSystem.IsWindows())
        {
            // Docker.DotNet шлёт attach без Connection:Upgrade — прокси Docker Desktop на Windows
            // не пробрасывает запись в stdin (dotnet/Docker.DotNet#618/#664). На Linux работает.
            Assert.Skip("stdin через attach не работает на Windows (Docker.DotNet#618); будет починен кастомным hijack-транспортом");
        }

        var result = await _service.RunContainerOnce(new RunContainerQuery
        {
            Image = TestImage,
            Cmd = ["sh", "-c", "cat; echo to-err >&2; exit 3"],
            Stdin = "from-stdin",
        }, CancellationToken.None);

        result.ExitCode.Should().Be(3);
        result.Stdout.Trim().Should().Be("from-stdin");
        result.Stderr.Trim().Should().Be("to-err");
    }

    [DockerFact]
    public async Task RunOnce_kills_on_timeout_and_removes_container()
    {
        var result = await _service.RunContainerOnce(new RunContainerQuery
        {
            Image = TestImage,
            Cmd = ["sleep", "60"],
            TimeoutSeconds = 2,
        }, CancellationToken.None);

        result.TimedOut.Should().BeTrue();

        var containers = await _service.ListContainers(new ListContainerQuery { Take = 1000 }, CancellationToken.None);
        containers.Items.Should().NotContain(c => c.Image.Contains("alpine") && c.State == "running" && c.Command.Contains("sleep 60"));
    }

    [DockerFact]
    public async Task CreateContainer_and_delete()
    {
        var name = $"mars-test-{Guid.NewGuid():N}"[..24];
        var created = await _service.CreateContainer(new CreateContainerQuery
        {
            Image = TestImage,
            Name = name,
            Cmd = ["echo", "created"],
        }, CancellationToken.None);

        try
        {
            created.ID.Should().NotBeNullOrEmpty();
            var found = await _service.GetContainerByName(name, CancellationToken.None);
            found.Should().NotBeNull();
        }
        finally
        {
            await _service.DeleteContainer(created.ID, CancellationToken.None);
        }

        (await _service.GetContainerByName(name, CancellationToken.None)).Should().BeNull();
    }

    [DockerFact]
    public async Task WaitContainer_returns_output_of_finished_container()
    {
        var created = await _service.CreateContainer(new CreateContainerQuery
        {
            Image = TestImage,
            Name = $"mars-test-{Guid.NewGuid():N}"[..24],
            Cmd = ["sh", "-c", "echo once-output; exit 7"],
        }, CancellationToken.None);

        try
        {
            await _service.StartContainer(created.ID, CancellationToken.None);

            var result = await _service.WaitContainer(created.ID, 30, CancellationToken.None);

            result.TimedOut.Should().BeFalse();
            result.ExitCode.Should().Be(7);
            result.Stdout.Trim().Should().Be("once-output");
        }
        finally
        {
            await _service.DeleteContainer(created.ID, CancellationToken.None);
        }
    }

    [DockerFact]
    public async Task WaitContainer_reports_timeout_while_still_running()
    {
        var created = await _service.CreateContainer(new CreateContainerQuery
        {
            Image = TestImage,
            Name = $"mars-test-{Guid.NewGuid():N}"[..24],
            Cmd = ["sleep", "60"],
        }, CancellationToken.None);

        try
        {
            await _service.StartContainer(created.ID, CancellationToken.None);

            var result = await _service.WaitContainer(created.ID, 2, CancellationToken.None);

            result.TimedOut.Should().BeTrue();
        }
        finally
        {
            await _service.StopContainer(created.ID, CancellationToken.None);
            await _service.DeleteContainer(created.ID, CancellationToken.None);
        }
    }

    [DockerFact]
    public async Task StartAndCapture_caches_output_and_removes_container()
    {
        var created = await _service.CreateContainer(new CreateContainerQuery
        {
            Image = TestImage,
            Name = $"mars-test-{Guid.NewGuid():N}"[..24],
            Cmd = ["sh", "-c", "echo capture-out; echo capture-err >&2; exit 5"],
        }, CancellationToken.None);

        var result = await _service.StartAndCapture(created.ID, 30, removeAfterExit: true, CancellationToken.None);

        result.TimedOut.Should().BeFalse();
        result.ExitCode.Should().Be(5);
        result.Stdout.Trim().Should().Be("capture-out");
        result.Stderr.Trim().Should().Be("capture-err");

        (await _service.GetContainer(created.ID, CancellationToken.None)).Should().BeNull();

        var cached = await _service.GetRunResult(created.ID);
        cached.Should().NotBeNull();
        cached!.ExitCode.Should().Be(5);
        cached.Stdout.Trim().Should().Be("capture-out");
        cached.Stderr.Trim().Should().Be("capture-err");
    }

    [DockerFact]
    public async Task ExecInContainer_runs_command_in_live_container()
    {
        var name = $"mars-test-{Guid.NewGuid():N}"[..24];
        var created = await _service.CreateContainer(new CreateContainerQuery
        {
            Image = TestImage,
            Name = name,
            Cmd = ["sleep", "60"],
        }, CancellationToken.None);

        try
        {
            await _service.StartContainer(created.ID, CancellationToken.None);

            var listed = await _service.ListContainers(new ListContainerQuery { Take = 1000 }, CancellationToken.None);
            listed.Items.Should().ContainSingle(c => c.ID == created.ID)
                .Which.StartedAt.Should().NotBeNull();

            var result = await _service.ExecInContainer(created.ID, new ExecContainerQuery
            {
                Cmd = ["echo", "exec-ok"],
            }, CancellationToken.None);

            result.ExitCode.Should().Be(0);
            result.Stdout.Trim().Should().Be("exec-ok");

            var logs = await _service.GetContainerLogs(created.ID, 50, CancellationToken.None);
            logs.Stdout.Should().NotBeNull();
        }
        finally
        {
            await _service.StopContainer(created.ID, CancellationToken.None);
            await _service.DeleteContainer(created.ID, CancellationToken.None);
        }
    }
}
