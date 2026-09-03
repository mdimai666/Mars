using FluentAssertions;
using Mars.Core.Exceptions;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Plugin.Contracts.Catalog;

namespace Mars.WebApiClient.Integration.Tests.Tests.Plugins;

/// <summary>Каталог в тестовом хосте выключен — клиентские методы маркетплейса отдают пустое/null.</summary>
public class MarketplaceClientTests : BaseWebApiClientTests
{
    public MarketplaceClientTests(ApplicationFixture appFixture) : base(appFixture)
    {
    }

    [IntegrationFact]
    public async void MarketplaceStatus_Request_Unauthorized()
    {
        var client = GetWebApiClient(true);

        var action = () => client.Plugin.MarketplaceStatus();

        await action.Should().ThrowAsync<UnauthorizedException>();
    }

    [IntegrationFact]
    public async Task MarketplaceStatus_CatalogDisabled_ReturnsDisabled()
    {
        var client = GetWebApiClient();

        var status = await client.Plugin.MarketplaceStatus();

        status.Enabled.Should().BeFalse();
    }

    [IntegrationFact]
    public async Task MarketplaceSearch_CatalogDisabled_ReturnsEmpty()
    {
        var client = GetWebApiClient();

        var result = await client.Plugin.MarketplaceSearch(new MarketplaceSearchRequest { Q = "telegram" });

        result.Total.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [IntegrationFact]
    public async Task MarketplacePlugin_CatalogDisabled_ReturnsNull()
    {
        var client = GetWebApiClient();

        var plugin = await client.Plugin.MarketplacePlugin("com.example.plugin");

        plugin.Should().BeNull();
    }

    [IntegrationFact]
    public async Task MarketplaceReviews_CatalogDisabled_ReturnsEmpty()
    {
        var client = GetWebApiClient();

        var reviews = await client.Plugin.MarketplaceReviews("com.example.plugin");

        reviews.Total.Should().Be(0);
        reviews.Items.Should().BeEmpty();
    }
}
