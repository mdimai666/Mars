using FluentAssertions;
using Flurl.Http;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.PxBlocks.Contracts.Dto;
using Microsoft.AspNetCore.Http;

namespace Mars.Integration.Tests.Controllers.PxBlocks;

/// <summary>
/// Смоук подключения PxBlocks в корне композиции (MarsWebAppStartup):
/// read-эндпоинты api/PxBlocks доступны, ядерные определения зарегистрированы.
/// Доменных контекстов в WebApp пока нет — REST запуска объявляют потребители.
/// </summary>
public class PxBlocksEndpointTests : ApplicationTests
{
    const string _apiUrl = "/api/PxBlocks";

    public PxBlocksEndpointTests(ApplicationFixture appFixture) : base(appFixture)
    {
    }

    [IntegrationFact]
    public async Task Definitions_ContainsCoreBlocks()
    {
        var client = AppFixture.GetClient(true);

        var res = await client.Request(_apiUrl, "Definitions").GetAsync();
        var result = await res.GetJsonAsync<PxDefinitionsResponse>();

        res.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.DefinitionsJson.Should().Contain("core.events.start");
        result.DefinitionsJson.Should().Contain("core.events.loop");
        result.Toolbox.Should().NotBeNull();
    }

    [IntegrationFact]
    public async Task Contexts_Empty_NoDomainContextsInWebApp()
    {
        var client = AppFixture.GetClient(true);

        var res = await client.Request(_apiUrl, "Contexts").GetAsync();
        var result = await res.GetJsonAsync<List<PxEditorContextInfo>>();

        res.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Should().BeEmpty();
    }

    [IntegrationFact]
    public async Task ContextDefinitions_UnknownName_Returns404()
    {
        var client = AppFixture.GetClient(true);

        var res = await client.Request(_apiUrl, "Contexts", "no-such-context")
                              .AllowAnyHttpStatus()
                              .GetAsync();

        res.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
}
