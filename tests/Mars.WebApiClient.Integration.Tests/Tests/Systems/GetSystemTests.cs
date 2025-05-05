using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.WebApiClient.Interfaces;
using FluentAssertions;

namespace Mars.WebApiClient.Integration.Tests.Tests.Systems;

public class GetSystemTests : BaseWebApiClientTests
{
    public GetSystemTests(ApplicationFixture appFixture) : base(appFixture)
    {
    }

    private async Task ActionShouldNotThrown<T>(Func<IMarsWebApiClient, Task<T>> action)
    {
        var act = () => action(GetWebApiClient());
        await act.Should().NotThrowAsync();
    }

    [IntegrationFact]
    public Task HealthCheck_Request_Success()
        => ActionShouldNotThrown(client => client.System.HealthCheck());

    [IntegrationFact]
    public Task HealthCheck2_Request_Success()
            => ActionShouldNotThrown(client => client.System.HealthCheck2());

    [IntegrationFact]
    public Task MemoryUsage_Request_Success()
        => ActionShouldNotThrown(client => client.System.MemoryUsage());

    [IntegrationFact]
    public Task AppUptime_Request_Success()
        => ActionShouldNotThrown(client => client.System.AppUptime());

    [IntegrationFact]
    public Task AppStartDateTime_Request_Success()
        => ActionShouldNotThrown(client => client.System.AppStartDateTime());

    [IntegrationFact]
    public Task SystemUptime_Request_Success()
        => ActionShouldNotThrown(client => client.System.SystemUptime());

    [IntegrationFact]
    public Task SystemUptimeMillis_Request_Success()
        => ActionShouldNotThrown(client => client.System.SystemUptimeMillis());

    [IntegrationFact]
    public Task SystemMinStat_Request_Success()
        => ActionShouldNotThrown(client => client.System.SystemMinStat());

    [IntegrationFact]
    public Task AboutSystem_Request_Success()
        => ActionShouldNotThrown(client => client.System.AboutSystem());

    [IntegrationFact]
    public Task HostCacheSettings_Request_Success()
        => ActionShouldNotThrown(client => client.System.HostCacheSettings());

}
