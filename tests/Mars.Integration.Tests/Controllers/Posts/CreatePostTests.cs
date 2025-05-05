using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Data.Entities;
using Mars.Host.Repositories;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.Posts;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
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

        using var ef = AppFixture.MarsDbContext();
        var metaFields = _fixture.CreateMany<MetaFieldEntity>(3).ToArray().ToList();
        var postType = ef.PostTypes.Include(s => s.MetaFields).First(s => s.TypeName == "post");
        postType.MetaFields = metaFields;
        ef.MetaFields.AddRange(metaFields);
        ef.SaveChanges();
        AppFixture.ServiceProvider.GetRequiredService<IMetaModelTypesLocator>().InvalidateCompiledMetaMtoModels();

        var metaValueCreateList = metaFields.Select(mf => _fixture.CreateSimpleCreateMetaValueRequest(mf.Id, mf.Type)).ToArray();

        var post = _fixture.Create<CreatePostRequest>() with
        {
            MetaValues = metaValueCreateList
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
}
