using AutoFixture;
using Mars.Host.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Contracts.Feedbacks;
using Mars.Test.Common.Constants;
using Mars.Test.Common.FixtureCustomizes;
using Mars.WebApiClient.Integration.Tests.GeneralTestAbstractions;
using FluentAssertions;

namespace Mars.WebApiClient.Integration.Tests.Tests.Feedbacks;

public class CreateFeedbackTests : BaseWebApiClientTests
{
    GeneralCreateTests<FeedbackEntity, CreateFeedbackRequest, Guid> _createTest;

    public CreateFeedbackTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());

        _createTest = new(this, (client, req) => client.Feedback.Create(req));
    }

    [IntegrationFact]
    public async Task CreateFeedback_UnauthorizedRequest_ShouldSuccess()
    {
        //Arrange
        var client = GetWebApiClient(true);
        var request = _fixture.Create<CreateFeedbackRequest>();

        //Act
        var result = await client.Feedback.Create(request);

        //Assert
        var entity = await GetEntity<FeedbackEntity>(result);
        entity.Should().NotBeNull();
        entity.AuthorizedUserId.Should().BeNull();
    }

    [IntegrationFact]
    public async Task CreateFeedback_AuthorizedRequest_ShouldSaveUser()
    {
        //Arrange
        var client = GetWebApiClient();
        var request = _fixture.Create<CreateFeedbackRequest>();

        //Act
        var result = await client.Feedback.Create(request);

        //Assert
        var entity = await GetEntity<FeedbackEntity>(result);
        entity.Should().NotBeNull();
        entity.AuthorizedUserId.Should().Be(UserConstants.TestUserId);
    }

    [IntegrationFact]
    public async Task CreateFeedback_ValidRequest_ShouldSuccess()
    {
        await _createTest.ValidRequest_ShouldSuccess();
    }

    [IntegrationFact]
    public void CreateFeedback_InvalidModelRequest_ValidateError()
    {
        _createTest.InvalidModelRequest_ValidateError(req => req with { Title = string.Empty }, "Title");
    }

}
