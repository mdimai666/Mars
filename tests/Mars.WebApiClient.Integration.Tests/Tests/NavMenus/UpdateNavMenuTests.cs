using Mars.Host.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Contracts.NavMenus;
using Mars.Test.Common.FixtureCustomizes;
using Mars.WebApiClient.Integration.Tests.GeneralTestAbstractions;

namespace Mars.WebApiClient.Integration.Tests.Tests.NavMenus;

public sealed class UpdateNavMenuTests : BaseWebApiClientTests
{
    GeneralUpdateTests<NavMenuEntity, UpdateNavMenuRequest> _updateTest;

    public UpdateNavMenuTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _updateTest = new(this, (client, req) => client.NavMenu.Update(req));

    }

    [IntegrationFact]
    public async Task UpdateNavMenu_Request_Unauthorized()
    {
        await _updateTest.ValidRequest_Unauthorized();
    }

    [IntegrationFact]
    public async Task UpdateNavMenu_ValidRequest_ShouldSuccess()
    {
        await _updateTest.ValidRequest_ShouldSuccess(req => req with { Title = "new title" });
    }

    [IntegrationFact]
    public void UpdateNavMenu_InvalidModelRequest_ValidateError()
    {
        _updateTest.InvalidModelRequest_ValidateError(req => req with { Slug = string.Empty }, "Slug");
    }
}
