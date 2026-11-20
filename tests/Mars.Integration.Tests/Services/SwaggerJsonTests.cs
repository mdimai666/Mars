using System.Text.Json.Nodes;
using FluentAssertions;
using Flurl.Http;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;

namespace Mars.Integration.Tests.Services;

/// <summary>
/// Swagger.json должен собираться и отдаваться без исключений: Swashbuckle скомпилирован
/// под конкретную мажорную версию Microsoft.OpenApi, и расхождение версий (пин в
/// Directory.Packages.props) падает в MissingMethodException при генерации документа.
/// </summary>
public sealed class SwaggerJsonTests : ApplicationTests
{
    public SwaggerJsonTests(ApplicationFixture appFixture) : base(appFixture)
    {
    }

    [IntegrationFact]
    public async Task SwaggerJson_GetDocument_ReturnsValidOpenApiJson()
    {
        //Arrange
        var client = AppFixture.GetClient();

        //Act
        var json = await client.Request("swagger/v1/swagger.json").GetStringAsync();

        //Assert
        var doc = JsonNode.Parse(json);
        doc.Should().NotBeNull();
        doc!["openapi"]!.GetValue<string>().Should().StartWith("3.");
        doc["info"]!["title"]!.GetValue<string>().Should().Be("API");
        var paths = doc["paths"]!.AsObject();
        paths.Should().NotBeEmpty();
    }
}
