using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Data.Entities;
using Mars.Host.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Contracts.PostCategoryTypes;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;

namespace Mars.Integration.Tests.Controllers.PostCategoryTypes;

/// <seealso cref="PostCategoryTypeController.Create(Shared.Contracts.PostCategoryTypes.CreatePostCategoryTypeRequest, CancellationToken)"/>
public sealed class GetPostCategoryTypeTests : ApplicationTests
{
    const string _apiUrl = "/api/PostCategoryType";

    public GetPostCategoryTypeTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task GetPostCategoryType_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostCategoryTypeController.Get);
        _ = nameof(PostCategoryTypeService.Get);
        var client = AppFixture.GetClient();

        var createdPostCategoryType = _fixture.Create<PostCategoryTypeEntity>();

        var ef = AppFixture.MarsDbContext();
        await ef.PostCategoryTypes.AddAsync(createdPostCategoryType);
        await ef.SaveChangesAsync();

        var postCategoryTypeId = createdPostCategoryType.Id;

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(postCategoryTypeId).GetJsonAsync<PostCategoryTypeDetailResponse>();

        //Assert
        result.Should().NotBeNull();
    }

    [IntegrationFact]
    public async Task GetPostCategoryType_InvalidModelRequest_Fail404()
    {
        //Arrange
        _ = nameof(PostCategoryTypeController.Get);
        _ = nameof(PostCategoryTypeService.Get);
        var client = AppFixture.GetClient();
        var invalidPostCategoryTypeId = Guid.NewGuid();

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(invalidPostCategoryTypeId)
                        .AllowAnyHttpStatus().SendAsync(HttpMethod.Get);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
}
