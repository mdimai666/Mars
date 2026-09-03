using FluentAssertions;
using Flurl.Http;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Plugin.Contracts.Catalog;
using Microsoft.AspNetCore.Http;

namespace Mars.Integration.Tests.Controllers.Plugins;

/// <summary>
/// В тестовом хосте каталог выключен (секция PluginCatalog не задана): витрина
/// отдаёт пустые ответы и 404, установка по nuget-id при этом не затрагивается.
/// </summary>
public class MarketplaceTests : ApplicationTests
{
    const string _apiUrl = "/api/Plugin/marketplace";

    public MarketplaceTests(ApplicationFixture appFixture) : base(appFixture)
    {
    }

    [IntegrationFact]
    public async Task Status_CatalogDisabled_ReturnsDisabled()
    {
        var client = AppFixture.GetClient();

        var result = await client.Request($"{_apiUrl}/status").GetJsonAsync<MarketplaceStatusResponse>();

        result.Enabled.Should().BeFalse();
    }

    [IntegrationFact]
    public async Task Status_Anonymous_Unauthorized()
    {
        var client = AppFixture.GetClient(true);

        var result = await client.Request($"{_apiUrl}/status").AllowAnyHttpStatus().GetAsync();

        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task Search_CatalogDisabled_ReturnsEmptyPage()
    {
        var client = AppFixture.GetClient();

        var result = await client.Request(_apiUrl)
                                 .AppendQueryParam(new { q = "telegram", recommended = true, sort = "rating", page = 1, take = 10 })
                                 .GetJsonAsync<CatalogPagedResponse<CatalogPluginDto>>();

        result.Total.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [IntegrationFact]
    public async Task Plugin_CatalogDisabled_Returns404()
    {
        var client = AppFixture.GetClient();

        var result = await client.Request($"{_apiUrl}/com.example.plugin").AllowAnyHttpStatus().GetAsync();

        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [IntegrationFact]
    public async Task Reviews_CatalogDisabled_ReturnsEmptyPage()
    {
        var client = AppFixture.GetClient();

        var result = await client.Request($"{_apiUrl}/com.example.plugin/reviews")
                                 .GetJsonAsync<CatalogPagedResponse<CatalogReviewDto>>();

        result.Total.Should().Be(0);
        result.Items.Should().BeEmpty();
    }
}
