using Mars.Host.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Contracts.Posts;
using Mars.Test.Common.FixtureCustomizes;
using Mars.WebApiClient.Integration.Tests.GeneralTestAbstractions;

namespace Mars.WebApiClient.Integration.Tests.Tests.PostJsons;

public class GetPostJsonTests : BaseWebApiClientTests
{
    GeneralGetTests<PostEntity, ListPostQueryRequest, TablePostQueryRequest, PostJsonResponse, PostJsonResponse> _getTest;

    public GetPostJsonTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());

        _getTest = new(
            this,
            (client, id) => client.PostJson.Get(id),
            (client, query) => client.PostJson.List(query, "post"),
            (client, query) => client.PostJson.ListTable(query, "post")
            );

    }

    [IntegrationFact]
    public async void GetPostJson_ValidRequest_ShouldSuccess()
    {
        _ = nameof(MarsWebApiClient.PostJson.Get);
        await _getTest.GetDetail_ValidRequest_ShouldSuccess();
    }

    [IntegrationFact]
    public void GetPostJson_NotExistEntity_Fail404ShouldReturnNullInsteadException()
    {
        _getTest.GetDetail_NotExistEntity_Fail404ShouldReturnNullInsteadException();
    }


    [IntegrationFact]
    public async void ListPostJson_ValidRequest_ShouldSuccess()
    {
        await _getTest.List_ValidRequest_ShouldSuccess(new(), new());
    }
}
