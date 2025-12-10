using System.Text.Json;
using System.Text.Json.Nodes;
using AutoFixture;
using FluentAssertions;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Contracts.PostJsons;
using Mars.Test.Common.FixtureCustomizes;
using Mars.WebApiClient.Integration.Tests.GeneralTestAbstractions;
using Mars.WebApiClient.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebApiClient.Integration.Tests.Tests.PostJsons;

public class CreatePostJsonTests : BaseWebApiClientTests
{
    GeneralCreateTests<PostEntity, CreatePostJsonRequest, PostJsonResponse> _createTest;

    public CreatePostJsonTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());

        _createTest = new(this, (client, req) => client.PostJson.Create(req));
    }

    [IntegrationFact]
    public async Task CreatePostJson_Request_Unauthorized()
    {
        await _createTest.ValidRequest_Unauthorized();
    }

    [IntegrationFact]
    public async Task CreatePostJson_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(IPostJsonServiceClient.Create);
        var mf = new MetaFieldEntity { Type = EMetaFieldType.Int, Key = "int1", Title = "Int 1" };
        SetupMetaFields([mf]);

        //Act
        await _createTest.ValidRequest_ShouldSuccess(req => req with
        {
            Meta = new Dictionary<string, JsonValue>
            {
                ["int1"] = JsonValue.Create(42)!
            }
        });
    }

    [IntegrationFact]
    public void CreatePostJson_InvalidModelRequest_ValidateError()
    {
        _createTest.InvalidModelRequest_ValidateError(req => req with { Title = string.Empty }, "Title");
    }

    [IntegrationFact]
    public async Task CreatePostJson_FromJson_ShouldSuccess()
    {
        //Arrange
        _ = nameof(IPostJsonServiceClient.Create);
        var client = GetWebApiClient();
        var mf = new MetaFieldEntity { Type = EMetaFieldType.Int, Key = "int1", Title = "Int 1" };
        SetupMetaFields([mf]);

        var json = """
            {
                "title": "Title 1",
                "type":"post",
                "meta": {
                    "int1": 123
                }
            }
            """;
        var request = JsonSerializer.Deserialize<CreatePostJsonRequest>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        //Act
        var result = await client.PostJson.Create(request);

        //Assert
        ((JsonElement)result.Meta["int1"]!).GetInt32().Should().Be(123);

    }

    private void SetupMetaFields(List<MetaFieldEntity> metaFields)
    {
        var ef = AppFixture.MarsDbContext();
        var postType = ef.PostTypes.Include(s => s.MetaFields).First(s => s.TypeName == "post");
        postType.MetaFields = metaFields;
        ef.MetaFields.AddRange(metaFields);
        ef.SaveChanges();
        AppFixture.ServiceProvider.GetRequiredService<IMetaModelTypesLocator>().InvalidateCompiledMetaMtoModels();
    }
}
