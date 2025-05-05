using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Data.Entities;
using Mars.Host.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;

namespace Mars.Integration.Tests.Controllers.Posts;

/// <seealso cref="PostController.Delete(Guid, CancellationToken)"/>
public class DeletePostTests : ApplicationTests
{
    const string _apiUrl = "/api/Post";

    public DeletePostTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task DeletePost_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostController.Delete);
        var client = AppFixture.GetClient();

        var post = _fixture.Create<PostEntity>();
        using var ef = AppFixture.MarsDbContext();
        await ef.Posts.AddAsync(post);
        await ef.SaveChangesAsync();
        var deletingId = post.Id;

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(deletingId).DeleteAsync().CatchUserActionError();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status200OK);

        var postTypeEntity = ef.Posts.FirstOrDefault(s => s.Id == deletingId);
        postTypeEntity.Should().BeNull();
    }

    [IntegrationFact]
    public async Task DeletePost_InvalidId_404NotFound()
    {
        //Arrange
        _ = nameof(PostController.Delete);
        var client = AppFixture.GetClient();
        var deletingId = Guid.NewGuid();

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(deletingId).AllowAnyHttpStatus().DeleteAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [IntegrationFact]
    public async Task DeletePost_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(PostController.Delete);
        _ = nameof(PostService.Delete);
        var client = AppFixture.GetClient(true);

        //Act
        var result = await client.Request(_apiUrl, Guid.NewGuid()).AllowAnyHttpStatus().DeleteAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }
}
