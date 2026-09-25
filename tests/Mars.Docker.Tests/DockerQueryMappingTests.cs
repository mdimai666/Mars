using FluentAssertions;
using Mars.Docker.Abstractions.Mapping;
using Mars.Docker.Contracts;

namespace Mars.Docker.Tests;

public class DockerQueryMappingTests
{
    [Fact]
    public void ToQuery_maps_create_container_request()
    {
        var request = new CreateContainerRequest
        {
            Image = "alpine:3.20",
            Name = "test",
            Cmd = ["echo", "hi"],
            Env = ["A=1"],
            Ports = [new ContainerPortBindingRequest { ContainerPort = 80, HostPort = 8080 }],
            WorkingDir = "/app",
            RestartPolicy = "unless-stopped",
            AutoRemove = true,
        };

        var query = request.ToQuery();

        query.Image.Should().Be("alpine:3.20");
        query.Name.Should().Be("test");
        query.Cmd.Should().Equal("echo", "hi");
        query.Env.Should().Equal("A=1");
        query.Ports.Should().ContainSingle(p => p.ContainerPort == 80 && p.HostPort == 8080 && p.Protocol == "tcp");
        query.WorkingDir.Should().Be("/app");
        query.RestartPolicy.Should().Be("unless-stopped");
        query.AutoRemove.Should().BeTrue();
    }

    [Fact]
    public void ToQuery_maps_run_once_request()
    {
        var request = new DockerRunOnceRequest
        {
            Image = "python:3.12-slim",
            Cmd = ["python", "-"],
            Stdin = "print(1)",
            TimeoutSeconds = 42,
            KeepContainer = true,
        };

        var query = request.ToQuery();

        query.Image.Should().Be("python:3.12-slim");
        query.Cmd.Should().Equal("python", "-");
        query.Stdin.Should().Be("print(1)");
        query.TimeoutSeconds.Should().Be(42);
        query.KeepContainer.Should().BeTrue();
    }

    [Fact]
    public void ToQuery_maps_exec_request()
    {
        var request = new DockerExecRequest
        {
            Cmd = ["ls", "-la"],
            Env = ["B=2"],
            User = "root",
            WorkingDir = "/tmp",
        };

        var query = request.ToQuery();

        query.Cmd.Should().Equal("ls", "-la");
        query.Env.Should().Equal("B=2");
        query.User.Should().Be("root");
        query.WorkingDir.Should().Be("/tmp");
    }

    [Fact]
    public void ToQuery_maps_list_requests()
    {
        var containers = new ListContainerRequest { Skip = 10, Take = 5, Search = "web", Sort = "-Created" }.ToQuery();
        containers.Skip.Should().Be(10);
        containers.Take.Should().Be(5);
        containers.Search.Should().Be("web");
        containers.Sort.Should().Be("-Created");

        var images = new ListImageRequest { Skip = 1, Take = 2, Search = "alpine" }.ToQuery();
        images.Skip.Should().Be(1);
        images.Take.Should().Be(2);
        images.Search.Should().Be("alpine");
    }
}
