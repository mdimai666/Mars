using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Data.Entities;
using Mars.Host.Repositories;
using Mars.Host.Services;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Posts;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Controllers.Posts;

public class GetPostTests : ApplicationTests
{
    const string _apiUrl = "/api/Post";

    public GetPostTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task GetPost_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostController.Get);
        _ = nameof(PostService.Get);
        var client = AppFixture.GetClient();

        var createdPost = _fixture.Create<PostEntity>();

        var ef = AppFixture.MarsDbContext();
        await ef.Posts.AddAsync(createdPost);
        await ef.SaveChangesAsync();

        var postTypeId = createdPost.Id;

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(postTypeId).GetJsonAsync<PostDetailResponse>();

        //Assert
        result.Should().NotBeNull();
    }

    [IntegrationFact]
    public async Task GetPostBySlug_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostController.GetBySlug);
        _ = nameof(PostService.GetDetailBySlug);
        var client = AppFixture.GetClient();

        var createdPost = _fixture.Create<PostEntity>();

        var ef = AppFixture.MarsDbContext();
        await ef.Posts.AddAsync(createdPost);
        await ef.SaveChangesAsync();

        //Act
        var typeName = "post";
        var result = await client.Request(_apiUrl, $"by-type/{typeName}/item/{createdPost.Slug}")
                                .GetJsonAsync<PostDetailResponse>();

        //Assert
        result.Should().NotBeNull();
    }

    [IntegrationFact]
    public async Task GetPost_InvalidModelRequest_Fail404()
    {
        //Arrange
        _ = nameof(PostController.Get);
        _ = nameof(PostService.Get);
        var client = AppFixture.GetClient();
        var invalidPostId = Guid.NewGuid();

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(invalidPostId)
                        .AllowAnyHttpStatus().SendAsync(HttpMethod.Get);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [IntegrationFact]
    public async Task ListPost_Request_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostController.List);
        _ = nameof(PostService.List);
        var client = AppFixture.GetClient();

        var createdPosts = _fixture.CreateMany<PostEntity>(3);

        var ef = AppFixture.MarsDbContext();
        await ef.Posts.AddRangeAsync(createdPosts);
        await ef.SaveChangesAsync();

        var expectCount = ef.Posts.Count();

        //Act
        var result = await client.Request(_apiUrl, "list/offset").GetJsonAsync<ListDataResult<PostListItemResponse>>();

        //Assert
        result.Should().NotBeNull();
        result.Items.Count().Should().Be(expectCount);
    }

    [IntegrationFact]
    public async Task ListPost_RequestByPostTypeName_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostController.List);
        _ = nameof(PostService.List);
        var client = AppFixture.GetClient();

        var createdPosts = _fixture.CreateMany<PostEntity>(3);

        var ef = AppFixture.MarsDbContext();
        await ef.Posts.AddRangeAsync(createdPosts);
        await ef.SaveChangesAsync();

        var expectCount = ef.Posts.Include(s => s.PostType).Count(s => s.PostType.TypeName == "post");

        //Act
        var result = await client.Request(_apiUrl, $"by-type/{("post")}/list/offset").GetJsonAsync<ListDataResult<PostListItemResponse>>();

        //Assert
        result.Should().NotBeNull();
        result.Items.Count().Should().Be(expectCount);
    }

    [IntegrationFact]
    public async Task ListPost_SearchRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostController.List);
        _ = nameof(PostService.List);
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
                                    .GetJsonAsync<ListDataResult<PostListItemResponse>>();

        //Assert
        result.Should().NotBeNull();
        result.Items.Count().Should().Be(1);
        result.Items.ElementAt(0).Id.Should().Be(expectPostId);
    }

    [IntegrationFact(Skip = "not yet")]
    public async Task GetPost__NonFilledMetaField_ShouldReturnBlankMetaValues()
    {
        //Arrange
        _ = nameof(PostController.Get);
        _ = nameof(PostService.Get);
        var client = AppFixture.GetClient();

        var ef = AppFixture.MarsDbContext();
        var metaFields = _fixture.CreateMany<MetaFieldEntity>(3).ToList();
        var postType = ef.PostTypes.Include(s => s.MetaFields).First(s => s.TypeName == "post");
        postType.MetaFields = metaFields;
        await ef.MetaFields.AddRangeAsync(metaFields);

        var createdPost = _fixture.Create<PostEntity>();

        await ef.Posts.AddAsync(createdPost);
        await ef.SaveChangesAsync();
        AppFixture.ServiceProvider.GetRequiredService<IMetaModelTypesLocator>().InvalidateCompiledMetaMtoModels();

        var postTypeId = createdPost.Id;

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(postTypeId).GetJsonAsync<PostDetailResponse>();

        //Assert
        //result.me.Should().NotBeNull();
        throw new NotImplementedException();
    }

    [IntegrationFact]
    public async Task GetEditModel_NonFilledMetaField_ShouldReturnBlankMetaValues()
    {
        //Arrange
        _ = nameof(PostController.GetEditModel);
        _ = nameof(PostService.GetEditModel);
        _ = nameof(PostRepository.GetPostEditDetail);
        var client = AppFixture.GetClient();

        var ef = AppFixture.MarsDbContext();
        var metaFields = _fixture.CreateMany<MetaFieldEntity>(3).ToList();
        var postType = ef.PostTypes.Include(s => s.MetaFields).First(s => s.TypeName == "post");
        postType.MetaFields = metaFields;
        await ef.MetaFields.AddRangeAsync(metaFields);

        var createdPost = _fixture.Create<PostEntity>();

        await ef.Posts.AddAsync(createdPost);
        await ef.SaveChangesAsync();
        AppFixture.ServiceProvider.GetRequiredService<IMetaModelTypesLocator>().InvalidateCompiledMetaMtoModels();

        //Act
        var result = await client.Request(_apiUrl, "edit", createdPost.Id).GetJsonAsync<PostEditViewModel>();

        //Assert
        result.Should().NotBeNull();
        result.Post.MetaValues.Should().HaveCount(metaFields.Count);
    }

}
