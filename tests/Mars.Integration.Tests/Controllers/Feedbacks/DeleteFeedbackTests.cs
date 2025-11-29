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

namespace Mars.Integration.Tests.Controllers.Feedbacks;

/// <seealso cref="FeedbackController.Delete(Guid, CancellationToken)"/>
public class DeleteFeedbackTests : ApplicationTests
{
    const string _apiUrl = "/api/Feedback";

    public DeleteFeedbackTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task DeleteFeedback_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(FeedbackController.Delete);
        var client = AppFixture.GetClient();

        var post = _fixture.Create<FeedbackEntity>();
        var ef = AppFixture.MarsDbContext();
        await ef.Feedbacks.AddAsync(post);
        await ef.SaveChangesAsync();
        var deletingId = post.Id;

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(deletingId).DeleteAsync().CatchUserActionError();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status200OK);

        var postTypeEntity = ef.Feedbacks.FirstOrDefault(s => s.Id == deletingId);
        postTypeEntity.Should().BeNull();
    }

    [IntegrationFact]
    public async Task DeleteFeedback_InvalidId_404NotFound()
    {
        //Arrange
        _ = nameof(FeedbackController.Delete);
        var client = AppFixture.GetClient();
        var deletingId = Guid.NewGuid();

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(deletingId).AllowAnyHttpStatus().DeleteAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [IntegrationFact]
    public async Task DeleteFeedback_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(FeedbackController.Delete);
        _ = nameof(FeedbackService.Delete);
        var client = AppFixture.GetClient(true);

        //Act
        var result = await client.Request(_apiUrl, Guid.NewGuid()).AllowAnyHttpStatus().DeleteAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }
}
