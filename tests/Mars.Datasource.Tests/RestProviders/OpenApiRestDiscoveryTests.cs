using System.Net;
using System.Text;
using FluentAssertions;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Mars.Datasource.Providers.Rest;
using Microsoft.OpenApi;

namespace Mars.Datasource.Tests.RestProviders;

/// <summary>
/// Каталог по документу OpenAPI: операции из <c>paths</c>, параметры из <c>parameters</c> и схемы тела,
/// базовый путь — из <c>servers</c>.
/// </summary>
public class OpenApiRestDiscoveryTests
{
    const string Document = """
    {
      "openapi": "3.0.0",
      "info": { "title": "Shop API", "version": "1.0" },
      "servers": [ { "url": "https://api.example.org/v1" } ],
      "paths": {
        "/orders": {
          "get": {
            "tags": [ "orders" ],
            "parameters": [
              { "name": "page", "in": "query", "required": false, "schema": { "type": "integer", "default": 1 } },
              { "name": "status", "in": "query", "schema": { "type": "string", "enum": [ "new", "paid" ], "description": "Статус заказа" } }
            ],
            "responses": { "200": { "description": "ok" } }
          },
          "post": {
            "tags": [ "orders" ],
            "requestBody": {
              "required": true,
              "content": {
                "application/json": {
                  "schema": {
                    "type": "object",
                    "required": [ "title" ],
                    "properties": {
                      "title": { "type": "string", "description": "Заголовок" },
                      "amount": { "type": "number" }
                    }
                  }
                }
              }
            },
            "responses": { "201": { "description": "created" } }
          }
        },
        "/orders/{id}": {
          "get": {
            "parameters": [ { "name": "id", "in": "path", "required": true, "schema": { "type": "string" } } ],
            "responses": { "200": { "description": "ok" } }
          }
        }
      }
    }
    """;

    static async Task<IReadOnlyList<DatasourceCatalogGroup>> BuildAsync()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(Document));

        var result = await OpenApiDocument.LoadAsync(stream);

        result.Document.Should().NotBeNull();
        result.Diagnostic?.Errors.Should().BeNullOrEmpty();

        return OpenApiRestDiscovery.Build(result.Document!);
    }

    [Fact]
    public async Task Build_GroupsByTag()
    {
        var groups = await BuildAsync();

        groups.Select(group => group.Name).Should().Equal("orders");

        groups.Single().Objects.Select(obj => obj.Id)
            .Should().BeEquivalentTo(["GET /v1/orders", "POST /v1/orders", "GET /v1/orders/{id}"]);
    }

    [Fact]
    public async Task Build_QueryParametersCarryTypeDefaultEnumAndDescription()
    {
        var groups = await BuildAsync();

        var parameters = groups.SelectMany(group => group.Objects)
            .Single(obj => obj.Id == "GET /v1/orders").Operation!.Parameters;

        parameters.Should().OnlyContain(parameter => parameter.In == DatasourceParameterIn.Query);

        var status = parameters.Single(parameter => parameter.Name == "status");
        status.Type.Should().Be("string");
        status.Enum.Should().Equal("new", "paid");
        status.Description.Should().Be("Статус заказа");

        parameters.Single(parameter => parameter.Name == "page").Default.Should().Be("1");
    }

    [Fact]
    public async Task Build_BodyPropertiesAreBodyParameters()
    {
        var groups = await BuildAsync();

        var parameters = groups.SelectMany(group => group.Objects)
            .Single(obj => obj.Id == "POST /v1/orders").Operation!.Parameters;

        parameters.Select(parameter => parameter.Name).Should().Equal("title", "amount");
        parameters.Should().OnlyContain(parameter => parameter.In == DatasourceParameterIn.Body);

        // Обязательность поля тела — в required схемы, а не у самого поля
        parameters.Single(parameter => parameter.Name == "title").Required.Should().BeTrue();
        parameters.Single(parameter => parameter.Name == "amount").Required.Should().BeFalse();
        parameters.Single(parameter => parameter.Name == "amount").Type.Should().Be("number");
    }

    [Fact]
    public async Task Build_PathParameterIsRequired()
    {
        var groups = await BuildAsync();

        var id = groups.SelectMany(group => group.Objects)
            .Single(obj => obj.Id == "GET /v1/orders/{id}").Operation!.Parameters.Single();

        id.In.Should().Be(DatasourceParameterIn.Path);
        id.Required.Should().BeTrue();
    }

    [Fact]
    public async Task Build_DraftQueryUsesServerBasePath()
    {
        var groups = await BuildAsync();

        var operation = groups.SelectMany(group => group.Objects).Single(obj => obj.Id == "GET /v1/orders/{id}");

        operation.ObjectType.Should().Be(DatasourceObjectType.Operation);
        operation.DefaultLanguage.Should().Be(DatasourceLanguage.Http);
        operation.DefaultQuery.Should().Be("GET {{baseUrl}}/v1/orders/{{id}}");
    }

    [Fact]
    public async Task Build_WithoutServersKeepsPathsAsIs()
    {
        var json = Document.Replace("""[ { "url": "https://api.example.org/v1" } ]""", "[]");

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var result = await OpenApiDocument.LoadAsync(stream);

        OpenApiRestDiscovery.Build(result.Document!)
            .SelectMany(group => group.Objects).Select(obj => obj.Id)
            .Should().Contain("GET /orders");
    }

    [Fact]
    public async Task DiscoverAsync_ReadsDocumentOverHttp()
    {
        string? requested = null;

        using var client = FakeHttp.Client(request =>
        {
            requested = request.RequestUri?.ToString();

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Document) };
        });

        var groups = await new OpenApiRestDiscovery().DiscoverAsync(client, "http://localhost:8080/swagger/v1/swagger.json");

        requested.Should().Be("http://localhost:8080/swagger/v1/swagger.json");
        groups.SelectMany(group => group.Objects).Select(obj => obj.Id).Should().Contain("GET /v1/orders");
    }

    [Fact]
    public async Task DiscoverAsync_WithoutAddressThrows()
    {
        using var client = FakeHttp.Client(HttpStatusCode.OK, Document);

        var discover = () => new OpenApiRestDiscovery().DiscoverAsync(client, "");

        await discover.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Не задан адрес документа OpenAPI*");
    }

    [Fact]
    public void Mode_IsOpenApi()
    {
        new OpenApiRestDiscovery().Mode.Should().Be(RestDiscovery.OpenApi);
        RestDiscovery.DefaultUrl(RestDiscovery.OpenApi).Should().Be("/swagger/v1/swagger.json");
    }
}
