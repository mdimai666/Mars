using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Data.Entities;
using Mars.Host.Services;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Controllers.PostTypes;

/// <seealso cref="PostTypeController.Delete(Guid, CancellationToken)"/>
public class DeletePostTypeTests : ApplicationTests
{
    const string _apiUrl = "/api/PostType";

    public DeletePostTypeTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task DeletePostType_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostTypeController.Delete);
        var client = AppFixture.GetClient();

        var postType = _fixture.Create<PostTypeEntity>();
        var ef = AppFixture.MarsDbContext();
        await ef.PostTypes.AddAsync(postType);
        await ef.SaveChangesAsync();
        var deletingId = postType.Id;

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(deletingId).DeleteAsync().CatchUserActionError();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status200OK);

        var postTypeEntity = ef.PostTypes.FirstOrDefault(s => s.Id == deletingId);
        postTypeEntity.Should().BeNull();
    }

    [IntegrationFact]
    public async Task DeletePostType_InvalidId_404NotFound()
    {
        //Arrange
        _ = nameof(PostTypeController.Delete);
        var client = AppFixture.GetClient();

        var deletingId = Guid.NewGuid();

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(deletingId).AllowAnyHttpStatus().DeleteAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [IntegrationFact]
    public async Task DeletePostType_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(PostTypeController.Delete);
        _ = nameof(PostTypeService.Delete);
        var client = AppFixture.GetClient(true);

        //Act
        var result = await client.Request(_apiUrl, Guid.NewGuid()).AllowAnyHttpStatus().DeleteAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task DeletePostType_InternalTypeCannotBeDelete_Should400()
    {
        //Arrange
        _ = nameof(PostTypeController.Delete);
        _ = nameof(PostTypeService.Delete);
        _ = nameof(DeletePostTypeQueryValidator);
        var client = AppFixture.GetClient();
        var metaModelLocator = AppFixture.ServiceProvider.GetRequiredService<IMetaModelTypesLocator>();
        var postTypeId = metaModelLocator.GetPostTypeByName("post").Id;

        //Act
        var result = await client.Request(_apiUrl, postTypeId).DeleteAsync().ReceiveValidationError();

        //Assert
        result.Errors.Should().HaveCount(1);
        result.Errors.First().Value.First().Should().Match("*is internal type and cannot be delete");
    }

    [IntegrationFact]
    public async Task DeleteManyPostType_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostTypeController.DeleteMany);
        var client = AppFixture.GetClient();

        var postTypes = _fixture.CreateMany<PostTypeEntity>().ToList();
        var ef = AppFixture.MarsDbContext();
        await ef.PostTypes.AddRangeAsync(postTypes);
        await ef.SaveChangesAsync();
        var deletingIds = postTypes.Select(s => s.Id).ToList();

        //Act
        var result = await client.Request(_apiUrl, "DeleteMany").AppendQueryParam(new { ids = deletingIds }).DeleteAsync().CatchUserActionError();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status200OK);

        var postTypesEntities = ef.Posts.Where(s => deletingIds.Contains(s.Id)).ToList();
        postTypesEntities.Should().BeEmpty();
    }
}
