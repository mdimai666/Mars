using System.Text.Json.Nodes;
using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.Posts;
using Mars.Shared.Contracts.PostTypes;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Controllers.Posts;

/// <summary>
/// Правило уникальности значения мета-поля (<c>unique</c> в Options.validators):
/// дубль значения у другого поста того же типа не проходит
/// </summary>
public sealed class PostUniqueValidatorTests : ApplicationTests
{
    const string _apiUrl = "/api/Post";

    public PostUniqueValidatorTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    static JsonNode UniqueOptions()
        => new JsonObject
        {
            ["validators"] = new JsonArray(new JsonObject
            {
                ["type"] = MetaFieldValidatorCatalog.Unique,
                ["params"] = new JsonObject(),
            }),
        };

    async Task<(string TypeName, Guid FieldId)> CreateTypeWithUniqueFieldAsync()
    {
        var field = _fixture.Create<CreateMetaFieldRequest>() with
        {
            Type = MetaFieldType.String,
            MinValue = null,
            MaxValue = null,
            Options = UniqueOptions(),
        };

        var typeRequest = _fixture.Create<CreatePostTypeRequest>() with { MetaFields = [field] };
        var postType = await AppFixture.ServiceProvider.GetRequiredService<IPostTypeService>()
                                       .Create(typeRequest.ToQuery(), default);

        return (postType.TypeName, field.Id);
    }

    CreatePostRequest NewPostRequest(string typeName, Guid fieldId, string value)
        => _fixture.Create<CreatePostRequest>() with
        {
            Type = typeName,
            Status = null,
            CategoryIds = [],
            MetaValues = [_fixture.CreateSimpleCreateMetaValueRequest(fieldId, EMetaFieldType.String) with { StringShort = value }],
        };

    [IntegrationFact]
    public async Task CreatePost_DuplicateUniqueValue_ShouldFail400()
    {
        //Arrange
        var (typeName, fieldId) = await CreateTypeWithUniqueFieldAsync();
        var client = AppFixture.GetClient();

        var first = await client.Request(_apiUrl).PostJsonAsync(NewPostRequest(typeName, fieldId, "dup-code")).CatchUserActionError();
        first.StatusCode.Should().Be(StatusCodes.Status201Created);

        //Act
        var validate = await client.Request(_apiUrl).PostJsonAsync(NewPostRequest(typeName, fieldId, "dup-code")).ReceiveValidationError();

        //Assert
        validate.Errors.Should().ContainKey("MetaValues");
        validate.Errors["MetaValues"].Should().Contain(m => m.Contains("уже занято"));
    }

    [IntegrationFact]
    public async Task UpdatePost_UniqueValue_SelfAllowed_OtherOccupied()
    {
        //Arrange
        var (typeName, fieldId) = await CreateTypeWithUniqueFieldAsync();
        var client = AppFixture.GetClient();
        var ef = AppFixture.MarsDbContext();

        var postA = await client.Request(_apiUrl).PostJsonAsync(NewPostRequest(typeName, fieldId, "aaa"))
                                .CatchUserActionError().ReceiveJson<PostDetailResponse>();
        var postB = await client.Request(_apiUrl).PostJsonAsync(NewPostRequest(typeName, fieldId, "bbb"))
                                .CatchUserActionError().ReceiveJson<PostDetailResponse>();
        var aValue = ef.PostMetaValues.AsNoTracking().First(v => v.PostId == postA.Id && v.MetaFieldId == fieldId);

        UpdatePostRequest UpdateRequest(string value)
            => _fixture.Create<UpdatePostRequest>() with
            {
                Id = postA.Id,
                Type = typeName,
                Status = null,
                CategoryIds = [],
                MetaValues = [_fixture.UpdateSimpleCreateMetaValueRequest(aValue.Id, fieldId, EMetaFieldType.String) with { StringShort = value }],
            };

        //Act/Assert — своё значение при обновлении не конфликтует
        var selfUpdate = await client.Request(_apiUrl).PutJsonAsync(UpdateRequest("aaa")).CatchUserActionError();
        selfUpdate.StatusCode.Should().Be(StatusCodes.Status200OK);

        //Act — значение другого поста занято
        var validate = await client.Request(_apiUrl).PutJsonAsync(UpdateRequest("bbb")).ReceiveValidationError();

        //Assert
        validate.Errors.Should().ContainKey("MetaValues");
        validate.Errors["MetaValues"].Should().Contain(m => m.Contains("уже занято"));
    }
}
