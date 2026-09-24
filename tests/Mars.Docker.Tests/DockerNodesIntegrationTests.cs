using Docker.DotNet;
using FluentAssertions;
using Mars.Docker.Abstractions.Dto;
using Mars.Docker.Contracts;
using Mars.Docker.Contracts.Nodes;
using Mars.Docker.Host.Nodes;
using Mars.Docker.Host.Options;
using Mars.Docker.Host.Services;
using Mars.Nodes.Abstractions;
using Mars.Nodes.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Mars.Docker.Tests;

/// <summary>
/// Ноды docker.* на живом демоне: pull → run → state → exec. Opt-in через MARS_DOCKER_TESTS=1.
/// </summary>
public class DockerNodesIntegrationTests : IDisposable
{
    private const string TestImage = "alpine:3.20";

    private readonly DockerClient _client = new DockerClientConfiguration().CreateClient();
    private readonly IDockerService _service;
    private readonly IServiceProvider _services;

    public DockerNodesIntegrationTests()
    {
        _service = new DockerService(_client,
            Options.Create(new DockerOptions { DefaultRunTimeoutSeconds = 120 }),
            new MemoryCache(new MemoryCacheOptions()));
        _services = new ServiceCollection().AddSingleton(_service).BuildServiceProvider();
    }

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }

    IRuntimeNodeScope CreateRns()
    {
        var rns = Substitute.For<IRuntimeNodeScope>();
        rns.ServiceProvider.Returns(_services);
        return rns;
    }

    static ExecutionParameters Parameters() => new(Guid.NewGuid(), Guid.NewGuid(), 0, CancellationToken.None, 0);

    [DockerFact]
    public async Task PullNode_pulls_image_and_sets_payload()
    {
        var node = new DockerPullNode { Image = "alpine", Tag = "3.20" };
        var impl = new DockerPullNodeImpl(node, CreateRns());

        NodeMsg? output = null;
        await impl.Execute(new NodeMsg(), (msg, _) => output = msg, Parameters());

        output.Should().NotBeNull();
        output!.Payload.Should().Be("alpine:3.20");
        (await _service.InspectImage(TestImage, CancellationToken.None)).Should().NotBeNull();
    }

    [DockerFact]
    public async Task RunNode_runs_once_and_returns_result()
    {
        var node = new DockerRunNode { Image = TestImage, Command = "echo node-hello" };
        var impl = new DockerRunNodeImpl(node, CreateRns());

        NodeMsg? output = null;
        await impl.Execute(new NodeMsg(), (msg, _) => output = msg, Parameters());

        output.Should().NotBeNull();
        var result = output!.Payload.Should().BeOfType<DockerRunResultResponse>().Subject;
        result.TimedOut.Should().BeFalse();
        result.ExitCode.Should().Be(0);
        result.Stdout.Trim().Should().Be("node-hello");
    }

    [DockerFact]
    public async Task StateNode_starts_and_stops_container_by_name()
    {
        var name = $"mars-node-{Guid.NewGuid():N}"[..24];
        var created = await _service.CreateContainer(new CreateContainerQuery
        {
            Image = TestImage,
            Name = name,
            Cmd = ["sleep", "300"],
        }, CancellationToken.None);

        try
        {
            var startNode = new DockerStateNode { ContainerName = name, Action = DockerStateAction.Start };
            NodeMsg? startOutput = null;
            await new DockerStateNodeImpl(startNode, CreateRns())
                .Execute(new NodeMsg(), (msg, _) => startOutput = msg, Parameters());
            startOutput!.Payload.Should().Be("running");

            var stopNode = new DockerStateNode { ContainerName = name, Action = DockerStateAction.Stop };
            NodeMsg? stopOutput = null;
            await new DockerStateNodeImpl(stopNode, CreateRns())
                .Execute(new NodeMsg(), (msg, _) => stopOutput = msg, Parameters());
            stopOutput!.Payload.Should().Be("exited");
        }
        finally
        {
            await _service.DeleteContainer(created.ID, CancellationToken.None);
        }
    }

    [DockerFact]
    public async Task ExecNode_runs_command_in_container()
    {
        var name = $"mars-node-{Guid.NewGuid():N}"[..24];
        var created = await _service.CreateContainer(new CreateContainerQuery
        {
            Image = TestImage,
            Name = name,
            Cmd = ["sleep", "300"],
        }, CancellationToken.None);

        try
        {
            await _service.StartContainer(created.ID, CancellationToken.None);

            var node = new DockerExecNode { ContainerName = name, Command = "echo exec-node-ok" };
            var impl = new DockerExecNodeImpl(node, CreateRns());

            NodeMsg? output = null;
            await impl.Execute(new NodeMsg(), (msg, _) => output = msg, Parameters());

            output.Should().NotBeNull();
            var result = output!.Payload.Should().BeOfType<DockerRunResultResponse>().Subject;
            result.ExitCode.Should().Be(0);
            result.Stdout.Trim().Should().Be("exec-node-ok");
        }
        finally
        {
            await _service.StopContainer(created.ID, CancellationToken.None);
            await _service.DeleteContainer(created.ID, CancellationToken.None);
        }
    }
}
