using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Data.Entities;
using Mars.Host.Repositories;
using Mars.Host.Repositories.Mappings;
using Mars.Host.Services;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.PostJsons;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Shared.Contracts.PostJsons;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Controllers.PostJsons;

public class CreatePostJsonTests : ApplicationTests
{
    const string _apiUrl = "/api/PostJson";

    public CreatePostJsonTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task CreatePostJson_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(PostJsonController.Create);
        _ = nameof(PostJsonService.Create);
        var client = AppFixture.GetClient(true);

        var postRequest = _fixture.Create<CreatePostJsonRequest>();

        //Act
        var result = await client.Request(_apiUrl).AllowAnyHttpStatus().PostJsonAsync(postRequest);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task CreatePostJson_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostJsonController.Create);
        _ = nameof(PostJsonService.Create);
        var client = AppFixture.GetClient();

        var ef = AppFixture.MarsDbContext();
        var metaFields = _fixture.CreateMany<MetaFieldEntity>(3).ToList();
        var postType = ef.PostTypes.Include(s => s.MetaFields).First(s => s.TypeName == "post");
        postType.MetaFields = metaFields;
        ef.MetaFields.AddRange(metaFields);
        ef.SaveChanges();
        AppFixture.ServiceProvider.GetRequiredService<IMetaModelTypesLocator>().InvalidateCompiledMetaMtoModels();

        var metaValuesQuery = metaFields.Select(s => ModifyMetaValueDetailQuery.GetBlank(s.ToDto()).SetMetaValue(_fixture)).ToList();

        var metaValuesCreateList = metaValuesQuery.ToDictionary(s => s.MetaField.Key, s => JsonValue.Create(s.GetValueSimple()));

        var post = _fixture.Create<CreatePostJsonRequest>() with
        {
            Meta = metaValuesCreateList!
        };

        //Act
        var res = await client.Request(_apiUrl).PostJsonAsync(post).CatchUserActionError();
        var result = await res.GetJsonAsync<PostJsonResponse>();

        //Assert
        ef.ChangeTracker.Clear();
        res.StatusCode.Should().Be(StatusCodes.Status201Created);
        result.Should().NotBeNull();
        var dbPost = ef.Posts.Include(s => s.MetaValues!)
                                .ThenInclude(s => s.MetaField)
                                .FirstOrDefault(s => s.Id == post.Id);
        dbPost.Should().NotBeNull();

        dbPost.Should().BeEquivalentTo(post, options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<CreatePostJsonRequest>()
            .Excluding(s => s.Meta)
            .ExcludingMissingMembers());
        dbPost.MetaValues.Should().AllSatisfy(e =>
        {
            var expectValue = metaValuesQuery.First(s => s.MetaField.Key == e.MetaField.Key);
            var current = e.Get();
            var expect = expectValue.GetValueSimple();
            current.Should().BeEquivalentTo(expect);

            e.ToDto().Should().BeEquivalentTo(expectValue, options => options
                .ComparingRecordsByValue()
                .ComparingByMembers<MetaValueDetailBase>()
                .Excluding(s => s.Id)
                .Excluding(s => s.MetaField)
                .ExcludingMissingMembers());

        });
    }

    [IntegrationFact]
    public async Task CreatePostJson_ValidateQueryValidator_ShouldFail()
    {
        //Arrange
        _ = nameof(PostRepository.Create);
        _ = nameof(PostJsonController.Create);
        _ = nameof(CreatePostQueryValidator);
        var client = AppFixture.GetClient();

        var post = _fixture.Create<CreatePostJsonRequest>() with { Status = "invalid_status_name" };

        //Act
        var validate = await client.Request(_apiUrl).PostJsonAsync(post).ReceiveValidationError();

        //Assert
        validate.Errors.Should().HaveCount(1);
        validate.Errors.ElementAt(0).Key.Should().Be(nameof(CreatePostJsonRequest.Status));
    }

    private void SetupMetaFields(List<MetaFieldEntity> metaFields)
    {
        var ef = AppFixture.MarsDbContext();
        //var metaFields = _fixture.CreateMany<MetaFieldEntity>(3).ToList();
        var postType = ef.PostTypes.Include(s => s.MetaFields).First(s => s.TypeName == "post");
        postType.MetaFields = metaFields;
        ef.MetaFields.AddRange(metaFields);
        ef.SaveChanges();
        AppFixture.ServiceProvider.GetRequiredService<IMetaModelTypesLocator>().InvalidateCompiledMetaMtoModels();
    }

    [IntegrationFact]
    public async Task CreatePostJson_PassSimpleJsonString_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostJsonController.Create);
        _ = nameof(PostJsonService.Create);
        _ = nameof(CreatePostJsonRequest);
        var client = AppFixture.GetClient();
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
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        //Act
        var res = await client.Request(_apiUrl).PostAsync(content).CatchUserActionError();
        var result = await res.GetJsonAsync<PostJsonResponse>();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status201Created);
        ((JsonElement)result.Meta["int1"]!).GetInt32().Should().Be(123);
    }

    [IntegrationFact]
    public async Task CreatePostJson_PassSimpleJsonStringWithInvalidProp_ShouldFail400()
    {
        //Arrange
        _ = nameof(PostJsonController.Create);
        _ = nameof(PostJsonService.Create);
        _ = nameof(CreatePostJsonRequest);
        _ = nameof(CreatePostJsonQueryValidator);
        var client = AppFixture.GetClient();
        var mf = new MetaFieldEntity { Type = EMetaFieldType.Int, Key = "int1", Title = "Int 1" };
        SetupMetaFields([mf]);

        var json = """
            {
                "title": "Title 1",
                "type":"post",
                "meta": {
                    "string1": 123
                }
            }
            """;
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        //Act
        var validate = await client.Request(_apiUrl).PostAsync(content).ReceiveValidationError();

        //Assert
        validate.Errors.ElementAt(0).Key.Should().Be(nameof(CreatePostJsonRequest.Meta));
        validate.Errors.ElementAt(0).Value.First().Should().Match("*keys*string1*not exist");
    }
}
