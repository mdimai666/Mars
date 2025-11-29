using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Data.Entities;
using Mars.Host.Repositories;
using Mars.Host.Services;
using Mars.Host.Shared.Dto.Feedbacks;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Shared.Contracts.Feedbacks;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;

namespace Mars.Integration.Tests.Controllers.Feedbacks;

/// <seealso cref="Mars.Controllers.FeedbackController"/>
public class UpdateFeedbackTests : ApplicationTests
{
    const string _apiUrl = "/api/Feedback";

    public UpdateFeedbackTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task UpdateFeedback_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(FeedbackController.Create);
        _ = nameof(FeedbackRepository.Update);
        var client = AppFixture.GetClient(true);

        var feedbackRequest = _fixture.Create<UpdateFeedbackRequest>();

        //Act
        var result = await client.Request(_apiUrl).AllowAnyHttpStatus().PutJsonAsync(feedbackRequest);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task UpdateFeedback_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(FeedbackController.Update);
        _ = nameof(FeedbackRepository.Update);
        var client = AppFixture.GetClient();

        var createdFeedback = _fixture.Create<FeedbackEntity>();
        var ef = AppFixture.MarsDbContext();
        ef.Feedbacks.Add(createdFeedback);
        ef.SaveChanges();
        ef.ChangeTracker.Clear();

        var post = new UpdateFeedbackRequest
        {
            Id = createdFeedback.Id,
            Type = FeedbackType.BugReport.ToString(),
        };

        //Act
        var res = await client.Request(_apiUrl).PutJsonAsync(post).CatchUserActionError();
        var result = await res.GetJsonAsync<FeedbackDetailResponse>();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Should().NotBeNull();

        ef.ChangeTracker.Clear();
        var dbFeedback = ef.Feedbacks.FirstOrDefault(s => s.Id == post.Id);
        dbFeedback.Should().NotBeNull();

        dbFeedback.Should().BeEquivalentTo(post, options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<UpdateFeedbackRequest>()
            .ExcludingMissingMembers());
    }

    [IntegrationFact]
    public async Task UpdateFeedback_InvalidModelRequest_ValidateError()
    {
        //Arrange
        _ = nameof(FeedbackController.Update);
        _ = nameof(FeedbackService.Update);
        var client = AppFixture.GetClient();

        var updateFeedbackRequest = _fixture.Create<UpdateFeedbackRequest>();
        updateFeedbackRequest = updateFeedbackRequest with
        {
            Id = Guid.NewGuid(),
            Type = "invalid_type",
        };

        //Act
        var result = await client.Request(_apiUrl).PutJsonAsync(updateFeedbackRequest).ReceiveValidationError();

        //Assert
        result.Should().NotBeNull();
        result.Errors.ValidateSatisfy(new()
        {
            [nameof(UpdateFeedbackRequest.Type)] = ["*feedback type*not allowed*"],
        });
    }
}
