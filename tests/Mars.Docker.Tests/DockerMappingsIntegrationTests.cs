using Docker.DotNet;
using FluentAssertions;
using Mars.Docker.Abstractions.Dto;
using Mars.Docker.Host.Mappings;
using Mars.Docker.Host.Options;
using Mars.Docker.Host.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Mars.Docker.Tests;

/// <summary>
/// Маппинги Docker.DotNet → Contracts на реальных данных демона:
/// nullable-поля (Labels, Node, Health, BindOptions и т.п.) не должны ронать ToResponse.
/// Opt-in через MARS_DOCKER_TESTS=1.
/// </summary>
public class DockerMappingsIntegrationTests : IDisposable
{
    private const string TestImage = "alpine:3.20";

    private readonly DockerClient _client = new DockerClientConfiguration().CreateClient();
    private readonly IDockerService _service;

    public DockerMappingsIntegrationTests()
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
    public async Task ListImages_mapping_handles_null_labels()
    {
        await _service.EnsureImage(TestImage, null, CancellationToken.None);

        var result = (await _service.ListImages(new ListImageQuery { Take = 1000 }, CancellationToken.None)).ToResponse();

        result.Items.Should().NotBeEmpty();
        var alpine = result.Items.Should().ContainSingle(i => i.RepoTags.Contains(TestImage)).Subject;
        alpine.Labels.Should().NotBeNull();
        alpine.RepoDigests.Should().NotBeNull();
        alpine.ID.Should().NotBeNullOrEmpty();
    }

    [DockerFact]
    public async Task ListImagesTable_mapping_returns_paging()
    {
        await _service.EnsureImage(TestImage, null, CancellationToken.None);

        var result = (await _service.ListImagesTable(new ListImageQuery { Take = 10 }, CancellationToken.None)).ToResponse();

        result.Items.Should().NotBeNull();
        result.Items.Should().AllSatisfy(i => i.RepoTags.Should().NotBeNull());
    }

    [DockerFact]
    public async Task ListContainers_mapping_handles_plain_container()
    {
        var created = await _service.CreateContainer(new CreateContainerQuery
        {
            Image = TestImage,
            Name = $"mars-map-{Guid.NewGuid():N}"[..24],
            Cmd = ["sleep", "30"],
        }, CancellationToken.None);

        try
        {
            var result = await _service.ListContainers(new ListContainerQuery { Take = 1000 }, CancellationToken.None);

            var mapped = result.Items.Should().ContainSingle(c => c.ID == created.ID).Subject;
            mapped.Labels.Should().NotBeNull();
            mapped.Ports.Should().NotBeNull();
            mapped.Mounts.Should().NotBeNull();
            mapped.NetworkSettings.Networks.Should().NotBeNull();
            mapped.State.Should().Be("created");
            mapped.StartedAt.Should().BeNull();
        }
        finally
        {
            await _service.DeleteContainer(created.ID, CancellationToken.None);
        }
    }

    [DockerFact]
    public async Task InspectContainer_mapping_handles_null_node_health_and_mount_options()
    {
        var created = await _service.CreateContainer(new CreateContainerQuery
        {
            Image = TestImage,
            Name = $"mars-map-{Guid.NewGuid():N}"[..24],
            Cmd = ["sleep", "30"],
        }, CancellationToken.None);

        try
        {
            var inspect = await _service.InspectContainer(created.ID, CancellationToken.None);
            inspect.Should().NotBeNull();

            var mapped = inspect!.ToResponse();

            mapped.ID.Should().Be(created.ID);
            mapped.State.Status.Should().Be("created");
            mapped.State.Health.Should().NotBeNull();
            mapped.Node.Should().NotBeNull();
            mapped.HostConfig.PortBindings.Should().NotBeNull();
            mapped.HostConfig.RestartPolicy.Should().NotBeNull();
            mapped.HostConfig.Devices.Should().NotBeNull();
            mapped.HostConfig.Ulimits.Should().NotBeNull();
            mapped.Config.Env.Should().NotBeNull();
            mapped.Config.ExposedPorts.Should().NotBeNull();
            mapped.Config.Volumes.Should().NotBeNull();
            mapped.Config.Healthcheck.Should().NotBeNull();
            mapped.NetworkSettings.Ports.Should().NotBeNull();
            mapped.NetworkSettings.Networks.Should().NotBeNull();
            mapped.Mounts.Should().NotBeNull();
        }
        finally
        {
            await _service.DeleteContainer(created.ID, CancellationToken.None);
        }
    }

    [DockerFact]
    public async Task InspectContainer_mapping_handles_published_ports()
    {
        var created = await _service.CreateContainer(new CreateContainerQuery
        {
            Image = TestImage,
            Name = $"mars-map-{Guid.NewGuid():N}"[..24],
            Cmd = ["sleep", "30"],
            Ports = [new Contracts.ContainerPortBindingRequest { ContainerPort = 80, HostPort = 18080 }],
            RestartPolicy = "unless-stopped",
        }, CancellationToken.None);

        try
        {
            var mapped = (await _service.InspectContainer(created.ID, CancellationToken.None))!.ToResponse();

            mapped.HostConfig.PortBindings.Should().ContainKey("80/tcp");
            mapped.HostConfig.PortBindings["80/tcp"].Should().ContainSingle(b => b.HostPort == "18080");
            mapped.Config.ExposedPorts.Should().ContainKey("80/tcp");
            mapped.HostConfig.RestartPolicy.Name.Should().Be(Contracts.RestartPolicyKindResponse.UnlessStopped);
        }
        finally
        {
            await _service.DeleteContainer(created.ID, CancellationToken.None);
        }
    }
}
