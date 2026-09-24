using FluentAssertions;
using Mars.Docker.Host.Services;

namespace Mars.Docker.Tests;

public class DockerImageNameTests
{
    [Theory]
    [InlineData("alpine", false)]
    [InlineData("library/alpine", false)]
    [InlineData("mdimai666/mars", false)]
    [InlineData("localhost/alpine", true)]
    [InlineData("localhost:5000/alpine", true)]
    [InlineData("ghcr.io/owner/app", true)]
    [InlineData("mirror.gcr.io/library/alpine", true)]
    [InlineData("registry.example.com:5000/team/app", true)]
    public void HasRegistry_detects_first_segment(string image, bool expected)
        => DockerImageName.HasRegistry(image).Should().Be(expected);

    [Theory]
    [InlineData("alpine", "alpine", "latest")]
    [InlineData("alpine:3.20", "alpine", "3.20")]
    [InlineData("python:3.12-slim", "python", "3.12-slim")]
    [InlineData("ghcr.io/owner/app:v1", "ghcr.io/owner/app", "v1")]
    [InlineData("localhost:5000/alpine", "localhost:5000/alpine", "latest")]
    [InlineData("localhost:5000/alpine:edge", "localhost:5000/alpine", "edge")]
    public void SplitTag_separates_last_colon_after_slash(string image, string expectedName, string expectedTag)
    {
        var (name, tag) = DockerImageName.SplitTag(image);
        name.Should().Be(expectedName);
        tag.Should().Be(expectedTag);
    }

    [Theory]
    [InlineData("alpine", "mirror.gcr.io", "mirror.gcr.io/alpine")]
    [InlineData("library/alpine", "mirror.gcr.io", "mirror.gcr.io/library/alpine")]
    [InlineData("ghcr.io/owner/app", "mirror.gcr.io", "ghcr.io/owner/app")]
    [InlineData("alpine", "", "alpine")]
    [InlineData("alpine", null, "alpine")]
    public void ApplyRegistry_prefixes_only_plain_names(string image, string? registry, string expected)
        => DockerImageName.ApplyRegistry(image, registry).Should().Be(expected);

    [Theory]
    [InlineData("alpine", "", "alpine:latest")]
    [InlineData("alpine:3.20", "", "alpine:3.20")]
    [InlineData("alpine", "mirror.gcr.io", "mirror.gcr.io/alpine:latest")]
    [InlineData("python:3.12-slim", "mirror.gcr.io", "mirror.gcr.io/python:3.12-slim")]
    public void ResolveFull_combines_registry_and_tag(string image, string registry, string expected)
        => DockerImageName.ResolveFull(image, registry).Should().Be(expected);
}
