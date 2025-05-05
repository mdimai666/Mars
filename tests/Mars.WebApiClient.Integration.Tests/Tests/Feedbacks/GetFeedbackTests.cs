using Mars.Host.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Contracts.Feedbacks;
using Mars.Test.Common.FixtureCustomizes;
using Mars.WebApiClient.Integration.Tests.GeneralTestAbstractions;

namespace Mars.WebApiClient.Integration.Tests.Tests.Feedbacks;

public class GetFeedbackTests : BaseWebApiClientTests
{
    GeneralGetTests<FeedbackEntity, ListFeedbackQueryRequest, TableFeedbackQueryRequest, FeedbackDetailResponse, FeedbackSummaryResponse> _getTest;

    public GetFeedbackTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());

        _getTest = new(
            this,
            (client, id) => client.Feedback.Get(id),
            (client, query) => client.Feedback.List(query),
            (client, query) => client.Feedback.ListTable(query)
            );

    }

    [IntegrationFact]
    public async void GetFeedback_ValidRequest_ShouldSuccess()
    {
        await _getTest.GetDetail_ValidRequest_ShouldSuccess();
    }

    [IntegrationFact]
    public void GetFeedback_NotExistEntity_Fail404ShouldReturnNullInsteadException()
    {
        _getTest.GetDetail_NotExistEntity_Fail404ShouldReturnNullInsteadException();
    }


    [IntegrationFact]
    public async void ListFeedback_ValidRequest_ShouldSuccess()
    {
        await _getTest.List_ValidRequest_ShouldSuccess(new(), new());
    }
}
