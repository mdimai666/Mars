using Mars.Host.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Contracts.Roles;
using Mars.Test.Common.FixtureCustomizes;
using Mars.WebApiClient.Integration.Tests.GeneralTestAbstractions;

namespace Mars.WebApiClient.Integration.Tests.Tests.Roles;

public class GetRoleTests : BaseWebApiClientTests
{
    GeneralGetTests<RoleEntity, ListRoleQueryRequest, TableRoleQueryRequest, RoleDetailResponse, RoleSummaryResponse> _getTest;

    public GetRoleTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());

        _getTest = new(
            this,
            (client, id) => client.Role.Get(id),
            (client, query) => client.Role.List(query),
            (client, query) => client.Role.ListTable(query)
            );

    }

    [IntegrationFact]
    public async void GetRole_Request_Unauthorized()
    {
        await _getTest.GetDetail_Request_Unauthorized();
    }

    [IntegrationFact]
    public async void GetRole_ValidRequest_ShouldSuccess()
    {
        await _getTest.GetDetail_ValidRequest_ShouldSuccess();
    }

    [IntegrationFact]
    public void GetRole_NotExistEntity_Fail404ShouldReturnNullInsteadException()
    {
        _getTest.GetDetail_NotExistEntity_Fail404ShouldReturnNullInsteadException();
    }


    [IntegrationFact]
    public async void ListRole_Request_Unauthorized()
    {
        await _getTest.List_Request_Unauthorized(new());
    }

    [IntegrationFact]
    public async void ListRole_ValidRequest_ShouldSuccess()
    {
        await _getTest.List_ValidRequest_ShouldSuccess(new(), new());
    }
}
