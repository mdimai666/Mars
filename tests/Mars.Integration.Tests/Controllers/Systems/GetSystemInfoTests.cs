using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Contracts.Systems;
using Mars.UseStartup;
using Microsoft.AspNetCore.Http;

namespace Mars.Integration.Tests.Controllers.Systems;

public class GetSystemInfoTests : ApplicationTests
{
    const string _apiUrl = "/api/System";

    public GetSystemInfoTests(ApplicationFixture appFixture) : base(appFixture)
    {
    }

    private async Task ActionShouldUnauthorized(string url)
        => (await AppFixture.GetClient(true)
                        .Request(_apiUrl, url)
                        .AllowAnyHttpStatus()
                        .GetAsync())
                    .StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

    [IntegrationFact]
    public async Task HealthCheck_Request_Success()
    {
        //Arrange
        _ = nameof(SystemController.HealthCheck);
        var client = AppFixture.GetClient(true);

        //Act
        var res = await client.Request(_apiUrl, "HealthCheck").GetAsync();
        var result = await res.GetStringAsync();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Should().Be("OK");
    }

    [IntegrationFact]
    public async Task HealthCheck2_Request_Success()
    {
        //Arrange
        _ = nameof(SystemController.HealthCheck2);
        var client = AppFixture.GetClient();

        //Act
        var res = await client.Request(_apiUrl, "HealthCheck2").GetAsync();
        var result = await res.GetStringAsync();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Should().Be("StartDateTime - " + MarsStartupInfo.StartDateTime.ToString("dd.MM.yyyy dd:mm:ss zz"));
    }

    [IntegrationFact]
    public async Task MemoryUsage_Request_Success()
    {
        //Arrange
        _ = nameof(SystemController.MemoryUsage);
        var client = AppFixture.GetClient();

        //Act
        var res = await client.Request(_apiUrl, "MemoryUsage").GetAsync();
        var result = await res.GetStringAsync();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Should().NotBeEmpty();
    }

    [IntegrationFact]
    public Task MemoryUsage_AnonimRequest_ShouldUnauthorized()
        => ActionShouldUnauthorized("MemoryUsage");

    [IntegrationFact]
    public async Task AppUptime_Request_Success()
    {
        //Arrange
        _ = nameof(SystemController.AppUptime);
        var client = AppFixture.GetClient();

        //Act
        var res = await client.Request(_apiUrl, "AppUptime").GetAsync();
        var result = await res.GetStringAsync();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Should().NotBeEmpty();
    }

    [IntegrationFact]
    public Task AppUptime_AnonimRequest_ShouldUnauthorized()
        => ActionShouldUnauthorized("AppUptime");

    [IntegrationFact]
    public async Task AppStartDateTime_Request_Success()
    {
        //Arrange
        _ = nameof(SystemController.AppStartDateTime);
        var client = AppFixture.GetClient();

        //Act
        var res = await client.Request(_apiUrl, "AppStartDateTime").GetAsync();
        var result = await res.GetJsonAsync<DateTime>();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Ticks.Should().BeGreaterThan(0);
    }

    [IntegrationFact]
    public Task AppStartDateTime_AnonimRequest_ShouldUnauthorized()
        => ActionShouldUnauthorized("AppStartDateTime");

    [IntegrationFact]
    public async Task SystemUptime_Request_Success()
    {
        //Arrange
        _ = nameof(SystemController.SystemUptime);
        var client = AppFixture.GetClient();

        //Act
        var res = await client.Request(_apiUrl, "SystemUptime").GetAsync();
        var result = await res.GetStringAsync();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Should().NotBeEmpty();
    }

    [IntegrationFact]
    public Task SystemUptime_AnonimRequest_ShouldUnauthorized()
        => ActionShouldUnauthorized("SystemUptime");

    [IntegrationFact]
    public async Task SystemUptimeMillis_Request_Success()
    {
        //Arrange
        _ = nameof(SystemController.SystemUptimeMillis);
        var client = AppFixture.GetClient();

        //Act
        var res = await client.Request(_apiUrl, "SystemUptimeMillis").GetAsync();
        var result = await res.GetJsonAsync<long>();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Should().BeGreaterThan(0);
    }

    [IntegrationFact]
    public Task SystemUptimeMillis_AnonimRequest_ShouldUnauthorized()
        => ActionShouldUnauthorized("SystemUptimeMillis");

    [IntegrationFact]
    public async Task SystemMinStat_Request_Success()
    {
        //Arrange
        _ = nameof(SystemController.SystemMinStat);
        var client = AppFixture.GetClient();

        //Act
        var res = await client.Request(_apiUrl, "SystemMinStat").GetAsync();
        var result = await res.GetJsonAsync<SystemMinStatResponse>();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Should().NotBeNull();
    }

    [IntegrationFact]
    public Task SystemMinStat_AnonimRequest_ShouldUnauthorized()
        => ActionShouldUnauthorized("SystemMinStat");

    [IntegrationFact]
    public async Task AboutSystem_Request_Success()
    {
        //Arrange
        _ = nameof(SystemController.AboutSystem);
        var client = AppFixture.GetClient();

        //Act
        var res = await client.Request(_apiUrl, "AboutSystem").GetAsync();
        var result = await res.GetJsonAsync<List<KeyValuePair<string, string>>>();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Should().NotBeNull();
    }

    [IntegrationFact]
    public Task AboutSystem_AnonimRequest_ShouldUnauthorized()
        => ActionShouldUnauthorized("AboutSystem");

    [IntegrationFact]
    public async Task HostCacheSettings_Request_Success()
    {
        //Arrange
        _ = nameof(SystemController.HostCacheSettings);
        var client = AppFixture.GetClient();

        //Act
        var res = await client.Request(_apiUrl, "HostCacheSettings").GetAsync();
        var result = await res.GetJsonAsync<List<KeyValuePair<string, string>>>();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Should().NotBeNull();
    }

    [IntegrationFact]
    public Task HostCacheSettings_AnonimRequest_ShouldUnauthorized()
        => ActionShouldUnauthorized("HostCacheSettings");
}
