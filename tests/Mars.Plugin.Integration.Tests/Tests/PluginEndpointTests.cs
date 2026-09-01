using FluentAssertions;
using Flurl.Http;
using Mars.Integration.Tests.Attributes;
using Mars.Options.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;
using PluginExample;
using PluginExample.Options;

namespace Mars.Plugin.Integration.Tests.Tests;

public class PluginEndpointTests : BasePluginTests
{
    public PluginEndpointTests(PluginApplicationFixture appFixture) : base(appFixture)
    {
    }

    [IntegrationFact]
    public async Task MapGet_PlainEndpoint_Responds()
    {
        //Arrange
        _ = nameof(PluginExamplePlugin.ConfigureWebApplication);
        var client = AppFixture.GetClient();

        //Act
        var result = await client.Request("api/PluginExample/Ping").GetStringAsync();

        //Assert
        result.Should().Be("pong");
    }

    [IntegrationFact]
    public async Task MapGet_EndpointWithDiParameter_ResolvesServiceAndResponds()
    {
        //Arrange
        _ = nameof(PluginExamplePlugin.ConfigureWebApplication);
        var client = AppFixture.GetClient();
        // значение задаётся в тесте: базовый сброс БД затирает опции, сохранённые при старте приложения
        AppFixture.ServiceProvider.GetRequiredService<IOptionService>()
                  .SaveOption(new PluginExampleOption1 { Value = "305" });

        //Act
        var result = await client.Request("api/PluginExample/OptionValue").GetStringAsync();

        //Assert
        result.Should().Be("305");
    }
}
