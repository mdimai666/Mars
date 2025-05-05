using Mars.Host.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Test.Common.FixtureCustomizes;
using Mars.WebApiClient.Integration.Tests.GeneralTestAbstractions;

namespace Mars.WebApiClient.Integration.Tests.Tests.Roles;

public sealed class DeleteRoleTests : BaseWebApiClientTests
{
    GeneralDeleteTests<RoleEntity, Guid> _deleteTest;

    public DeleteRoleTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());

        _deleteTest = new(this, (client, req) => client.Role.Delete(req));
    }

    [IntegrationFact]
    public async Task DeleteRole_Request_Unauthorized()
    {
        await _deleteTest.ValidRequest_Unauthorized();
    }

    [IntegrationFact]
    public async Task DeleteRole_ValidRequest_ShouldSuccess()
    {
        await _deleteTest.ValidRequest_ShouldSuccess();
    }

    [IntegrationFact]
    public async Task DeleteRole_NotExistEntity_ThrowNotFoundException()
    {
        await _deleteTest.NotExistEntity_ThrowNotFoundException();
    }
}
