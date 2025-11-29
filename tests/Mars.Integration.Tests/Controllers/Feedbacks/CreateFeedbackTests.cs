using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Repositories;
using Mars.Host.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Shared.Contracts.Feedbacks;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Mars.Integration.Tests.Controllers.Feedbacks;

/// <seealso cref="Mars.Controllers.FeedbackController"/>
public class CreateFeedbackTests : ApplicationTests
{
    const string _apiUrl = "/api/Feedback";

    public CreateFeedbackTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task CreateFeedback_RequestAnonim_ShouldSuccess()
    {
        //Arrange
        _ = nameof(FeedbackRepository.Create);
        _ = nameof(FeedbackController.Create);
        var client = AppFixture.GetClient(true);

        var feedback = _fixture.Create<CreateFeedbackRequest>();

        //Act
        var resultId = await client.Request(_apiUrl).PostJsonAsync(feedback).CatchUserActionError().ReceiveJson<Guid?>();

        //Assert
        resultId.Should().NotBeNull();
        var ef = AppFixture.MarsDbContext();
        var dbFeedback = ef.Feedbacks.FirstOrDefault(s => s.Id == resultId);
        dbFeedback.Should().NotBeNull();
        dbFeedback.Title.Should().Be(feedback.Title);
        dbFeedback.Content.Should().Be(feedback.Content);
        dbFeedback.AuthorizedUserId.Should().BeNull();
    }

    [IntegrationFact]
    public async Task CreateFeedback_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(FeedbackRepository.Create);
        _ = nameof(FeedbackController.Create);
        var client = AppFixture.GetClient();

        var feedback = _fixture.Create<CreateFeedbackRequest>();

        //Act
        var res = await client.Request(_apiUrl).PostJsonAsync(feedback).CatchUserActionError();
        var resultId = await res.GetJsonAsync<Guid?>();

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status201Created);
        resultId.Should().NotBeNull();
        var ef = AppFixture.MarsDbContext();
        var dbFeedback = ef.Feedbacks.AsNoTracking().Include(s => s.AuthorizedUser).FirstOrDefault(s => s.Id == resultId);
        dbFeedback.Should().NotBeNull();
        dbFeedback.Title.Should().Be(feedback.Title);
        dbFeedback.Content.Should().Be(feedback.Content);
        dbFeedback.AuthorizedUserId.Should().NotBeNull();
        dbFeedback.AuthorizedUser.Should().NotBeNull();
    }

    [IntegrationFact]
    public async Task CreateFeedback_InvalidModelRequest_ValidateError()
    {
        //Arrange
        _ = nameof(FeedbackController.Create);
        _ = nameof(FeedbackService.Create);
        var client = AppFixture.GetClient();

        var request = _fixture.Create<CreateFeedbackRequest>();
        request = request with
        {
            Title = string.Empty,
        };

        //Act
        var result = await client.Request(_apiUrl).PostJsonAsync(request).ReceiveValidationError();

        //Assert
        result.Should().NotBeNull();
        result.Errors.ValidateSatisfy(new()
        {
            [nameof(FeedbackDetailResponse.Title)] = ["The Title field is required."],
        });
    }

    [IntegrationFact]
    public async Task CreateFeedback_EmptyEmail_ShouldSuccess()
    {
        //Arrange
        _ = nameof(FeedbackController.Create);
        _ = nameof(FeedbackService.Create);
        var client = AppFixture.GetClient();

        var request = _fixture.Create<CreateFeedbackRequest>();
        request = request with
        {
            Email = null,
        };

        //Act
        var result = await client.Request(_apiUrl).PostJsonAsync(request);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status201Created);
    }
}
