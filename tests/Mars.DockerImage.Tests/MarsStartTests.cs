using FluentAssertions;
using Flurl.Http;
using Mars.DockerImage.Tests.Fixtures;
using Microsoft.AspNetCore.Http;

namespace Mars.DockerImage.Tests;

public class MarsStartTests : IClassFixture<MarsFixture>
{
    private readonly MarsFixture _fixture;

    public MarsStartTests(MarsFixture fixture)
    {
        _fixture = fixture;
    }

    [DockerContainerFact]
    public async Task MarsStart_EmptyDb_SucceedsAsync()
    {
        var req = await _fixture.Client.Request("/dev").AllowAnyHttpStatus().GetAsync();

        req.StatusCode.Should().Be(StatusCodes.Status200OK);
    }
}
