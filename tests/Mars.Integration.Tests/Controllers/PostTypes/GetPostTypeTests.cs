using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Data.Entities;
using Mars.Host.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Contracts.PostTypes;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;

namespace Mars.Integration.Tests.Controllers.PostTypes;

/// <seealso cref="PostTypeController.Create(Shared.Contracts.PostTypes.CreatePostTypeRequest, CancellationToken)"/>
public sealed class GetPostTypeTests : ApplicationTests
{
    const string _apiUrl = "/api/PostType";

    public GetPostTypeTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task GetPostType_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostTypeController.Get);
        _ = nameof(PostTypeService.Get);
        var client = AppFixture.GetClient();

        var createdPostType = _fixture.Create<PostTypeEntity>();

        using var ef = AppFixture.MarsDbContext();
        await ef.PostTypes.AddAsync(createdPostType);
        await ef.SaveChangesAsync();

        var postTypeId = createdPostType.Id;

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(postTypeId).GetJsonAsync<PostTypeDetailResponse>();

        //Assert
        result.Should().NotBeNull();
    }

    [IntegrationFact]
    public async Task GetPostType_InvalidModelRequest_Fail404()
    {
        //Arrange
        _ = nameof(PostTypeController.Get);
        _ = nameof(PostTypeService.Get);
        var client = AppFixture.GetClient();
        var invalidPostTypeId = Guid.NewGuid();

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(invalidPostTypeId)
                        .AllowAnyHttpStatus().SendAsync(HttpMethod.Get);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
}
