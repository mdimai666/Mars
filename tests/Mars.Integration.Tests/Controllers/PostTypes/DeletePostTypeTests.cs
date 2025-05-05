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
        using var ef = AppFixture.MarsDbContext();
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
}
