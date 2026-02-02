using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Data.Entities;
using Mars.Host.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Common;
using Mars.Shared.Contracts.PostJsons;
using Mars.Shared.Contracts.Posts;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;

namespace Mars.Integration.Tests.Controllers.PostJsons;

public class GetPostJsonTests : ApplicationTests
{
    const string _apiUrl = "/api/PostJson";

    public GetPostJsonTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task GetPostJson_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostJsonController.Get);
        _ = nameof(PostJsonService.GetDetail);
        var client = AppFixture.GetClient();

        var createdPost = _fixture.Create<PostEntity>();

        var ef = AppFixture.MarsDbContext();
        await ef.Posts.AddAsync(createdPost);
        await ef.SaveChangesAsync();

        var postTypeId = createdPost.Id;

        //Act
        var result = await client.Request(_apiUrl, postTypeId).GetJsonAsync<PostJsonResponse>();

        //Assert
        result.Should().NotBeNull();
    }

    [IntegrationFact]
    public async Task GetPostJsonBySlug_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostJsonController.GetBySlug);
        _ = nameof(PostJsonService.GetDetailBySlug);
        var client = AppFixture.GetClient();

        var createdPost = _fixture.Create<PostEntity>();

        var ef = AppFixture.MarsDbContext();
        await ef.Posts.AddAsync(createdPost);
        await ef.SaveChangesAsync();

        //Act
        var typeName = "post";
        var result = await client.Request(_apiUrl, $"by-type/{typeName}/item/{createdPost.Slug}")
                                .GetJsonAsync<PostJsonResponse>();

        //Assert
        result.Should().NotBeNull();
    }

    [IntegrationFact]
    public async Task GetPostJson_InvalidModelRequest_Fail404()
    {
        //Arrange
        _ = nameof(PostJsonController.Get);
        _ = nameof(PostJsonService.GetDetail);
        var client = AppFixture.GetClient();
        var invalidPostId = Guid.NewGuid();

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(invalidPostId)
                        .AllowAnyHttpStatus().SendAsync(HttpMethod.Get);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [IntegrationFact]
    public async Task ListPostJson_Request_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostJsonController.List);
        _ = nameof(PostJsonService.List);
        var client = AppFixture.GetClient();

        var createdPosts = _fixture.CreateMany<PostEntity>(3);

        var ef = AppFixture.MarsDbContext();
        await ef.Posts.AddRangeAsync(createdPosts);
        await ef.SaveChangesAsync();

        var expectCount = createdPosts.Count();

        //Act
        var result = await client.Request(_apiUrl, $"by-type/{("post")}/list/offset").GetJsonAsync<ListDataResult<PostJsonResponse>>();

        //Assert
        result.Should().NotBeNull();
        result.Items.Count().Should().BeGreaterThanOrEqualTo(expectCount);
    }

    [IntegrationFact]
    public async Task ListPostJson_SearchRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostJsonController.List);
        _ = nameof(PostJsonService.List);
        var client = AppFixture.GetClient();

        var searchString = $"{Guid.NewGuid()}";
        var searchTitleString = $"nonUsePart_{searchString}";

        var createdPosts = _fixture.CreateMany<PostEntity>(3);
        createdPosts.ElementAt(0).Title = searchTitleString;

        var ef = AppFixture.MarsDbContext();
        await ef.Posts.AddRangeAsync(createdPosts);
        await ef.SaveChangesAsync();

        var expectPostId = createdPosts.ElementAt(0).Id;

        var request = new ListPostQueryRequest() { Search = searchString };

        //Act
        var result = await client.Request(_apiUrl, $"by-type/{("post")}/list/offset")
                                    .AppendQueryParam(request)
                                    .GetJsonAsync<ListDataResult<PostJsonResponse>>();

        //Assert
        result.Should().NotBeNull();
        result.Items.Count().Should().Be(1);
        result.Items.ElementAt(0).Id.Should().Be(expectPostId);
    }

    [IntegrationFact(Skip = "not yet")]
    public async Task GetPostJson__NonFilledMetaField_ShouldReturnBlankMetaValues()
    {
        throw new NotImplementedException();
    }

}
