using ExternalServices.Integration.Tests.MarsDocker.Fixtures;
using FluentAssertions;
using Flurl.Http;
using Mars.Integration.Tests.Attributes;
using Microsoft.AspNetCore.Http;

namespace Mars.DockerContainer.Tests;

public class MarsStartTests : IClassFixture<MarsFixture>
{
    private readonly MarsFixture _fixture;

    public MarsStartTests(MarsFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset().Wait();
    }

    [IntegrationFact(Skip = MarsFixture.SkipTest)]
    public async Task MarsStart_EmptyDb_ShouldSuccessAsync()
    {
        var req = await _fixture.Client.Request("/dev").AllowAnyHttpStatus().GetAsync();

        req.StatusCode.Should().Be(StatusCodes.Status200OK);
    }
}
