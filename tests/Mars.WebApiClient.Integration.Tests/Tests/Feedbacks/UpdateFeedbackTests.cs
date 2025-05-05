using Mars.Host.Data.Entities;
using Mars.Host.Shared.Dto.Feedbacks;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Contracts.Feedbacks;
using Mars.Test.Common.FixtureCustomizes;
using Mars.WebApiClient.Integration.Tests.GeneralTestAbstractions;

namespace Mars.WebApiClient.Integration.Tests.Tests.Feedbacks;

public sealed class UpdateFeedbackTests : BaseWebApiClientTests
{
    GeneralUpdateTests<FeedbackEntity, UpdateFeedbackRequest, FeedbackDetailResponse> _updateTest;

    public UpdateFeedbackTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _updateTest = new(this, (client, req) => client.Feedback.Update(req));

    }

    [IntegrationFact]
    public async Task UpdateFeedback_Request_Unauthorized()
    {
        await _updateTest.ValidRequest_Unauthorized();
    }

    [IntegrationFact]
    public async Task UpdateFeedback_ValidRequest_ShouldSuccess()
    {
        await _updateTest.ValidRequest_ShouldSuccess(req => req with { Type = FeedbackType.BugReport.ToString() });
    }

    [IntegrationFact]
    public void UpdateFeedback_InvalidModelRequest_ValidateError()
    {
        _updateTest.InvalidModelRequest_ValidateError(req => req with { Type = "invalid_type" }, "Type");
    }
}
