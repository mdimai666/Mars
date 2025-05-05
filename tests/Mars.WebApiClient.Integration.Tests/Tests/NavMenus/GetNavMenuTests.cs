using Mars.Host.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Contracts.NavMenus;
using Mars.Test.Common.FixtureCustomizes;
using Mars.WebApiClient.Integration.Tests.GeneralTestAbstractions;

namespace Mars.WebApiClient.Integration.Tests.Tests.NavMenus;

public class GetNavMenuTests : BaseWebApiClientTests
{
    GeneralGetTests<NavMenuEntity, ListNavMenuQueryRequest, TableNavMenuQueryRequest, NavMenuDetailResponse, NavMenuSummaryResponse> _getTest;

    public GetNavMenuTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());

        _getTest = new(
            this,
            (client, id) => client.NavMenu.Get(id),
            (client, query) => client.NavMenu.List(query),
            (client, query) => client.NavMenu.ListTable(query)
            );

    }

    [IntegrationFact]
    public async void GetNavMenu_Request_Unauthorized()
    {
        await _getTest.GetDetail_Request_Unauthorized();
    }

    [IntegrationFact]
    public async void GetNavMenu_ValidRequest_ShouldSuccess()
    {
        await _getTest.GetDetail_ValidRequest_ShouldSuccess();
    }

    [IntegrationFact]
    public void GetNavMenu_NotExistEntity_Fail404ShouldReturnNullInsteadException()
    {
        _getTest.GetDetail_NotExistEntity_Fail404ShouldReturnNullInsteadException();
    }


    [IntegrationFact]
    public async void ListNavMenu_Request_Unauthorized()
    {
        await _getTest.List_Request_Unauthorized(new());
    }

    [IntegrationFact]
    public async void ListNavMenu_ValidRequest_ShouldSuccess()
    {
        await _getTest.List_ValidRequest_ShouldSuccess(new(), new());
    }
}
