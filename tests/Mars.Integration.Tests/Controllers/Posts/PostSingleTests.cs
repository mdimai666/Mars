using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Cms.Abstractions.Dto.PostTypes;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Contracts.Posts;
using Mars.Cms.Contracts.PostTypes;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Controllers.Posts;

/// <summary>
/// Single-тип (фича <see cref="PostTypeConstants.Features.Single"/>):
/// единственная запись создаётся при первом открытии, второй пост и удаление запрещены.
/// </summary>
public sealed class PostSingleTests : ApplicationTests
{
    const string _apiUrl = "/api/Post";

    public PostSingleTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    async Task<PostTypeDetail> CreateTypeAsync(IReadOnlyCollection<string> features, params CreateMetaFieldRequest[] fields)
    {
        var typeRequest = _fixture.Create<CreatePostTypeRequest>() with
        {
            MetaFields = fields,
            PostStatusList = [],
            EnabledFeatures = features,
            ImageFieldKey = null,
        };

        return await AppFixture.ServiceProvider.GetRequiredService<IPostTypeService>()
                               .Create(typeRequest.ToQuery(), default);
    }

    CreatePostRequest NewPostRequest(string typeName)
        => _fixture.Create<CreatePostRequest>() with
        {
            Type = typeName,
            Status = null,
            CategoryIds = [],
            MetaValues = [],
        };

    [IntegrationFact]
    public async Task GetOrCreateSingle_CreatesOnFirstOpen_ReturnsSameOnSecond()
    {
        //Arrange
        var postType = await CreateTypeAsync([PostTypeConstants.Features.Single]);
        var client = AppFixture.GetClient();

        //Act
        var first = await client.Request($"{_apiUrl}/single/{postType.TypeName}")
                                  .GetAsync().CatchUserActionError().ReceiveJson<PostDetailResponse>();
        var second = await client.Request($"{_apiUrl}/single/{postType.TypeName}")
                                   .GetAsync().CatchUserActionError().ReceiveJson<PostDetailResponse>();

        //Assert
        first.Title.Should().Be(postType.Title);
        first.Slug.Should().NotBeEmpty();
        first.Type.Should().Be(postType.TypeName);
        second.Id.Should().Be(first.Id);
    }

    [IntegrationFact]
    public async Task GetOrCreateSingle_MaterializesFieldDefaults()
    {
        //Arrange
        var field = _fixture.Create<CreateMetaFieldRequest>() with
        {
            Type = MetaFieldType.String,
            MinValue = null,
            MaxValue = null,
            Options = null,
            Disabled = false,
            Hidden = false,
            Default = new MetaFieldDefaultValue { StringShort = "single-default" },
        };
        var postType = await CreateTypeAsync([PostTypeConstants.Features.Single], field);
        var client = AppFixture.GetClient();

        //Act
        var single = await client.Request($"{_apiUrl}/single/{postType.TypeName}")
                                   .GetAsync().CatchUserActionError().ReceiveJson<PostDetailResponse>();

        //Assert
        var ef = AppFixture.MarsDbContext();
        var value = ef.PostMetaValues.AsNoTracking()
                                      .FirstOrDefault(v => v.PostId == single.Id && v.MetaFieldId == field.Id);
        value.Should().NotBeNull();
        value!.StringShort.Should().Be("single-default");
    }

    [IntegrationFact]
    public async Task CreateSecondPost_SingleType_Fails400()
    {
        //Arrange
        var postType = await CreateTypeAsync([PostTypeConstants.Features.Single]);
        var client = AppFixture.GetClient();
        _ = await client.Request($"{_apiUrl}/single/{postType.TypeName}")
                        .GetAsync().CatchUserActionError().ReceiveJson<PostDetailResponse>();

        //Act
        var validate = await client.Request(_apiUrl)
                                   .PostJsonAsync(NewPostRequest(postType.TypeName))
                                   .ReceiveValidationError();

        //Assert
        validate.Errors.Should().ContainKey("Type");
        validate.Errors["Type"].Should().Contain(m => m.Contains("единственную запись"));
    }

    [IntegrationFact]
    public async Task DeleteSinglePost_Fails400()
    {
        //Arrange
        var postType = await CreateTypeAsync([PostTypeConstants.Features.Single]);
        var client = AppFixture.GetClient();
        var single = await client.Request($"{_apiUrl}/single/{postType.TypeName}")
                                   .GetAsync().CatchUserActionError().ReceiveJson<PostDetailResponse>();

        //Act
        var validate = await client.Request($"{_apiUrl}/{single.Id}")
                                   .DeleteAsync()
                                   .ReceiveValidationError();

        //Assert
        validate.Errors.Should().ContainKey("Id");
        validate.Errors["Id"].Should().Contain(m => m.Contains("удаление запрещено"));
    }

    [IntegrationFact]
    public async Task EnableSingle_TypeWithTwoPosts_Fails400()
    {
        //Arrange
        var postType = await CreateTypeAsync([]);
        var client = AppFixture.GetClient();

        var firstPost = await client.Request(_apiUrl).PostJsonAsync(NewPostRequest(postType.TypeName))
                                    .CatchUserActionError();
        firstPost.StatusCode.Should().Be(StatusCodes.Status201Created);
        var secondPost = await client.Request(_apiUrl).PostJsonAsync(NewPostRequest(postType.TypeName))
                                     .CatchUserActionError();
        secondPost.StatusCode.Should().Be(StatusCodes.Status201Created);

        var update = _fixture.Create<UpdatePostTypeRequest>() with
        {
            Id = postType.Id,
            TypeName = postType.TypeName,
            EnabledFeatures = [PostTypeConstants.Features.Single],
            ImageFieldKey = null,
            MetaFields = [],
            PostStatusList = [],
        };

        //Act
        var validate = await client.Request("/api/PostType")
                                   .PutJsonAsync(update)
                                   .ReceiveValidationError();

        //Assert
        validate.Errors.Should().ContainKey("EnabledFeatures");
        validate.Errors["EnabledFeatures"].Should().Contain(m => m.Contains("Единственная запись"));
    }
}
