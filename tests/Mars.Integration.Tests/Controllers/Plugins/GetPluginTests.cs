using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Plugin;
using Mars.Plugin.Abstractions;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Plugins;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using static Mars.Plugin.ApplicationPluginExtensions;

namespace Mars.Integration.Tests.Controllers.Plugins;

public class GetPluginTests : ApplicationTests
{
    const string _apiUrl = "/api/Plugin";
    private readonly IPluginService _pluginService;

    public GetPluginTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _pluginService = appFixture.ServiceProvider.GetRequiredService<IPluginService>();
    }

    [IntegrationFact]
    public async Task ListPlugin_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(PluginController.ListTable);
        _ = nameof(IPluginService.ListTable);
        var client = AppFixture.GetClient(true);

        //Act
        var result = await client.Request(_apiUrl, "ListTable").AllowAnyHttpStatus().GetAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task ListPlugin_Request_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PluginController.ListTable);
        _ = nameof(IPluginService.ListTable);
        var client = AppFixture.GetClient();

        _fixture.AddTestPlugin();

        var request = new ListPluginQueryRequest();

        //Act
        var result = await client.Request(_apiUrl, "ListTable")
            .AppendQueryParam(request)
            .GetJsonAsync<PagingResult<PluginInfoResponse>>();

        //Assert
        result.Should().NotBeNull();
        result.Items.Count().Should().Be(1);
    }
}

public static class PluginTestsExtensions
{
    public static void AddTestPlugin(this IFixture _fixture)
    {
        var pluginInfo = _fixture.Create<PluginInfo>();
        var pluginSettings = _fixture.Create<PluginSettings>();

        var pluginData = new PluginData(false, false, pluginSettings, null!, pluginInfo);

        ApplicationPluginExtensions.Plugins.Add(pluginData);
    }
}
