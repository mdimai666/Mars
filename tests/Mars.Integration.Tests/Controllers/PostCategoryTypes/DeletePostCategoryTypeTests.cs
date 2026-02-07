using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Data.Entities;
using Mars.Host.Services;
using Mars.Host.Shared.Dto.PostCategoryTypes;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;

namespace Mars.Integration.Tests.Controllers.PostCategoryTypes;

/// <seealso cref="PostCategoryTypeController.Delete(Guid, CancellationToken)"/>
public class DeletePostCategoryTypeTests : ApplicationTests
{
    const string _apiUrl = "/api/PostCategoryType";

    public DeletePostCategoryTypeTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task DeletePostCategoryType_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostCategoryTypeController.Delete);
        var client = AppFixture.GetClient();

        var postCategoryType = _fixture.Create<PostCategoryTypeEntity>();
        var ef = AppFixture.MarsDbContext();
        await ef.PostCategoryTypes.AddAsync(postCategoryType);
        await ef.SaveChangesAsync();
        var deletingId = postCategoryType.Id;

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(deletingId).DeleteAsync().CatchUserActionError();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status200OK);

        var postCategoryTypeEntity = ef.PostCategoryTypes.FirstOrDefault(s => s.Id == deletingId);
        postCategoryTypeEntity.Should().BeNull();
    }

    [IntegrationFact]
    public async Task DeletePostCategoryType_InvalidId_404NotFound()
    {
        //Arrange
        _ = nameof(PostCategoryTypeController.Delete);
        var client = AppFixture.GetClient();

        var deletingId = Guid.NewGuid();

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(deletingId).AllowAnyHttpStatus().DeleteAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [IntegrationFact]
    public async Task DeletePostCategoryType_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(PostCategoryTypeController.Delete);
        _ = nameof(PostCategoryTypeService.Delete);
        var client = AppFixture.GetClient(true);

        //Act
        var result = await client.Request(_apiUrl, Guid.NewGuid()).AllowAnyHttpStatus().DeleteAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task DeletePostCategoryType_TryDeleteDefaultPostCategoryType_ShouldValidationError()
    {
        //Arrange
        _ = nameof(PostCategoryTypeController.Delete);
        _ = nameof(DeletePostCategoryTypeQueryValidator);
        var client = AppFixture.GetClient();

        var ef = AppFixture.MarsDbContext();
        var defaultPostCategoryType = ef.PostCategoryTypes.First(s => s.TypeName == "default");

        //Act
        var validate = await client.Request(_apiUrl).AppendPathSegment(defaultPostCategoryType.Id).DeleteAsync().ReceiveValidationError();

        //Assert
        validate.Errors.Should().HaveCount(1);
        validate.Errors.ElementAt(0).Key.Should().Be(nameof(PostCategoryTypeSummary.TypeName));
        validate.Errors[nameof(PostCategoryTypeSummary.TypeName)].First().Should().Match("*internal type and cannot be delete");

        ef.PostCategoryTypes.Any(s => s.Id == defaultPostCategoryType.Id).Should().BeTrue();
    }

    [IntegrationFact]
    public async Task DeleteManyPostCategoryType_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(PostCategoryTypeController.DeleteMany);
        var client = AppFixture.GetClient();

        var postCategoryTypes = _fixture.CreateMany<PostCategoryTypeEntity>().ToList();
        var ef = AppFixture.MarsDbContext();
        await ef.PostCategoryTypes.AddRangeAsync(postCategoryTypes);
        await ef.SaveChangesAsync();
        var deletingIds = postCategoryTypes.Select(s => s.Id).ToList();

        //Act
        var result = await client.Request(_apiUrl, "DeleteMany").AppendQueryParam(new { ids = deletingIds }).DeleteAsync().CatchUserActionError();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status200OK);

        var postCategoryTypeEntity = ef.PostCategoryTypes.Any(s => deletingIds.Contains(s.Id));
        postCategoryTypeEntity.Should().BeFalse();
    }
}
