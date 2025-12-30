using Mars.Host.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Test.Common.FixtureCustomizes;
using Mars.WebApiClient.Integration.Tests.GeneralTestAbstractions;

namespace Mars.WebApiClient.Integration.Tests.Tests.NavMenus;

public sealed class DeleteNavMenuTests : BaseWebApiClientTests
{
    GeneralDeleteTests<NavMenuEntity, Guid> _deleteTest;

    public DeleteNavMenuTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());

        _deleteTest = new(this, (client, req) => client.NavMenu.Delete(req), (client, req) => client.NavMenu.DeleteMany(req));
    }

    [IntegrationFact]
    public async Task DeleteNavMenu_Request_Unauthorized()
    {
        await _deleteTest.ValidRequest_Unauthorized();
    }

    [IntegrationFact]
    public async Task DeleteNavMenu_ValidRequest_ShouldSuccess()
    {
        await _deleteTest.ValidRequest_ShouldSuccess();
    }

    [IntegrationFact]
    public async Task DeleteNavMenu_NotExistEntity_ThrowNotFoundException()
    {
        await _deleteTest.NotExistEntity_ThrowNotFoundException();
    }

    [IntegrationFact]
    public async Task DeleteManyNavMenu_ValidRequest_ShouldSuccess()
    {
        await _deleteTest.DeleteMany_ValidRequest_ShouldSuccess();
    }
}
