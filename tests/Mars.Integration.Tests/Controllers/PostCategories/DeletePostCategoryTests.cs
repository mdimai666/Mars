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

namespace Mars.Integration.Tests.Controllers.PostCategories;

/// <seealso cref="PostCategoryController.Delete(Guid, CancellationToken)"/>
public class DeletePostCategoryTests : ApplicationTests
{
    const string _apiUrl = "/api/PostCategory";

    public DeletePostCategoryTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task DeletePostCategory_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostCategoryController.Delete);
        var client = AppFixture.GetClient();

        var post = _fixture.Create<PostCategoryEntity>();
        var ef = AppFixture.MarsDbContext();
        await ef.PostCategories.AddAsync(post);
        await ef.SaveChangesAsync();
        var deletingId = post.Id;

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(deletingId).DeleteAsync().CatchUserActionError();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status200OK);

        var postTypeEntity = ef.PostCategories.FirstOrDefault(s => s.Id == deletingId);
        postTypeEntity.Should().BeNull();
    }

    [IntegrationFact]
    public async Task DeletePostCategory_InvalidId_404NotFound()
    {
        //Arrange
        _ = nameof(PostCategoryController.Delete);
        var client = AppFixture.GetClient();
        var deletingId = Guid.NewGuid();

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(deletingId).AllowAnyHttpStatus().DeleteAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [IntegrationFact]
    public async Task DeletePostCategory_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(PostCategoryController.Delete);
        _ = nameof(PostCategoryService.Delete);
        var client = AppFixture.GetClient(true);

        //Act
        var result = await client.Request(_apiUrl, Guid.NewGuid()).AllowAnyHttpStatus().DeleteAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task DeleteManyPostCategory_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostCategoryController.DeleteMany);
        var client = AppFixture.GetClient();

        var postCategories = _fixture.CreateMany<PostCategoryEntity>().ToList();
        var ef = AppFixture.MarsDbContext();
        await ef.PostCategories.AddRangeAsync(postCategories);
        await ef.SaveChangesAsync();
        var deletingIds = postCategories.Select(s => s.Id).ToList();

        //Act
        var result = await client.Request(_apiUrl, "DeleteMany").AppendQueryParam(new { ids = deletingIds }).DeleteAsync().CatchUserActionError();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status200OK);

        var postCategoryEntities = ef.PostCategories.Where(s => deletingIds.Contains(s.Id)).ToList();
        postCategoryEntities.Should().BeEmpty();
    }
}
