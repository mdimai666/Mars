using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Data.Entities;
using Mars.Host.Repositories;
using Mars.Host.Shared.Dto.Posts;
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
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Controllers.Posts;

/// <seealso cref="Mars.Controllers.PostController"/>
public sealed class CreatePostTests : ApplicationTests
{
    const string _apiUrl = "/api/Post";

    public CreatePostTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task CreatePost_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(PostController.Create);
        var client = AppFixture.GetClient(true);

        var postRequest = _fixture.Create<CreatePostRequest>();

        //Act
        var result = await client.Request(_apiUrl).AllowAnyHttpStatus().PostJsonAsync(postRequest);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task CreatePost_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostRepository.Create);
        _ = nameof(PostController.Create);
        var client = AppFixture.GetClient();

        var ef = AppFixture.MarsDbContext();
        var metaFields = _fixture.CreateMany<MetaFieldEntity>(3).ToList();
        var postType = ef.PostTypes.Include(s => s.MetaFields).First(s => s.TypeName == "post");
        postType.MetaFields = metaFields;
        ef.MetaFields.AddRange(metaFields);
        var categories = _fixture.CreateMany<PostCategoryEntity>(3).ToList();
        ef.PostCategories.AddRange(categories);
        if (!postType.EnabledFeatures.Contains(PostTypeConstants.Features.Category))
            postType.EnabledFeatures.Add(PostTypeConstants.Features.Category);
        ef.SaveChanges();
        AppFixture.ServiceProvider.GetRequiredService<IMetaModelTypesLocator>().InvalidateCompiledMetaMtoModels();

        var metaValueCreateList = metaFields.Select(mf => _fixture.CreateSimpleCreateMetaValueRequest(mf.Id, mf.Type)).ToArray();

        var post = _fixture.Create<CreatePostRequest>() with
        {
            MetaValues = metaValueCreateList,
            CategoryIds = categories.Take(2).Select(s => s.Id).ToList(),
        };

        //Act
        var res = await client.Request(_apiUrl).PostJsonAsync(post).CatchUserActionError();
        var result = await res.GetJsonAsync<PostDetailResponse>();

        //Assert
        ef.ChangeTracker.Clear();
        res.StatusCode.Should().Be(StatusCodes.Status201Created);
        result.Should().NotBeNull();
        var dbPost = ef.Posts.Include(s => s.MetaValues!)
                                    .ThenInclude(s => s.MetaField)
                                .Include(s => s.Categories)
                                .FirstOrDefault(s => s.Id == post.Id);
        dbPost.Should().NotBeNull();

        dbPost.Should().BeEquivalentTo(post, options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<CreatePostRequest>()
            .Excluding(s => s.MetaValues)
            .ExcludingMissingMembers());
        dbPost.MetaValues.Should().AllSatisfy(e =>
        {
            var req = post.MetaValues.First(s => s.Id == e.Id);
            e.Should().BeEquivalentTo(req, options => options
                .ComparingRecordsByValue()
                .ComparingByMembers<CreateMetaValueRequest>()
                //.Excluding(s => s.DateTime)
                .ExcludingMissingMembers());
            //e.DateTime.Date.ToString("g").Should().Be(req.DateTime.Date.ToString("g"));

        });
        dbPost.Categories!.Select(s => s.Id).Should().BeEquivalentTo(post.CategoryIds);
    }

    [IntegrationFact]
    public async Task CreatePost_ValidateQueryValidator_ShouldFail()
    {
        //Arrange
        _ = nameof(PostRepository.Create);
        _ = nameof(PostController.Create);
        _ = nameof(CreatePostQueryValidator);
        var client = AppFixture.GetClient();

        var post = _fixture.Create<CreatePostRequest>() with { Status = "invalid_status_name" };

        //Act
        var validate = await client.Request(_apiUrl).PostJsonAsync(post).ReceiveValidationError();

        //Assert
        validate.Errors.Should().HaveCount(1);
        validate.Errors.ElementAt(0).Key.Should().Be(nameof(CreatePostRequest.Status));
    }

    [IntegrationFact(Skip = "on test mode Kestrel settings overrided")]
    public async Task CreatePost_BodyTooLarge_ShouldReturnPayloadTooLarge()
    {
        //Arrange
        var client = AppFixture.GetClient();

        // Создаём объект, который гарантированно превышает 10 МБ
        var largeText = new string('A', 116 * 1024 * 1024); // 16 MB
        var request = new
        {
            Title = "Large body test",
            Content = largeText
        };

        //Act
        var response = await client.Request(_apiUrl).AllowAnyHttpStatus().PostJsonAsync(request);

        /*
         curl -X POST http://localhost:5003/api/Post -H "Content-Type: application/json" --data-binary "@.\some_big_file.bin" -H "Authorization: Bearer eyJhbG..."
         */

        //Assert
        if (response.StatusCode == 400)
        {
            var validateErrros = await response.GetJsonAsync<ValidationProblemDetails>();
        }
        response.StatusCode.Should().Be((int)System.Net.HttpStatusCode.RequestEntityTooLarge);
    }

    [IntegrationFact]
    public async Task CreatePost_ForDisabledType_ShouldFail400()
    {
        //Arrange
        _ = nameof(PostRepository.Create);
        _ = nameof(PostController.Create);
        _ = nameof(GeneralPostQueryValidator);
        var client = AppFixture.GetClient();

        var postTypeRequest = _fixture.Create<CreatePostTypeRequest>().ToQuery() with { Disabled = true };
        await AppFixture.ServiceProvider.GetRequiredService<IPostTypeService>().Create(postTypeRequest, default);

        var post = _fixture.Create<CreatePostRequest>() with { Type = postTypeRequest.TypeName };

        //Act
        var validate = await client.Request(_apiUrl).PostJsonAsync(post).ReceiveValidationError();

        //Assert
        validate.Errors.Should().HaveCount(1);
        validate.Errors.ElementAt(0).Key.Should().Be(nameof(CreatePostRequest.Type));
        validate.Errors.ElementAt(0).Value.Should().Contain($"post type '{postTypeRequest.TypeName}' is disabled");
    }

}
