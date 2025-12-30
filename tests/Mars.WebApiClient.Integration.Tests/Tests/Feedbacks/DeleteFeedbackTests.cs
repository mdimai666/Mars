using Mars.Host.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Test.Common.FixtureCustomizes;
using Mars.WebApiClient.Integration.Tests.GeneralTestAbstractions;

namespace Mars.WebApiClient.Integration.Tests.Tests.Feedbacks;

public sealed class DeleteFeedbackTests : BaseWebApiClientTests
{
    GeneralDeleteTests<FeedbackEntity, Guid> _deleteTest;

    public DeleteFeedbackTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());

        _deleteTest = new(this, (client, req) => client.Feedback.Delete(req), (client, req) => client.Feedback.DeleteMany(req));
    }

    [IntegrationFact]
    public async Task DeleteFeedback_Request_Unauthorized()
    {
        await _deleteTest.ValidRequest_Unauthorized();
    }

    [IntegrationFact]
    public async Task DeleteFeedback_ValidRequest_ShouldSuccess()
    {
        await _deleteTest.ValidRequest_ShouldSuccess();
    }

    [IntegrationFact]
    public async Task DeleteFeedback_NotExistEntity_ThrowNotFoundException()
    {
        await _deleteTest.NotExistEntity_ThrowNotFoundException();
    }

    [IntegrationFact]
    public async Task DeleteManyFeedback_ValidRequest_ShouldSuccess()
    {
        await _deleteTest.DeleteMany_ValidRequest_ShouldSuccess();
    }
}
