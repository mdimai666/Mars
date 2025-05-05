using Mars.Host.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Contracts.NavMenus;
using Mars.Test.Common.FixtureCustomizes;
using Mars.WebApiClient.Integration.Tests.GeneralTestAbstractions;

namespace Mars.WebApiClient.Integration.Tests.Tests.NavMenus;

public class CreateNavMenuTests : BaseWebApiClientTests
{
    GeneralCreateTests<NavMenuEntity, CreateNavMenuRequest, Guid> _createTest;

    public CreateNavMenuTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());

        _createTest = new(this, (client, req) => client.NavMenu.Create(req));
    }

    [IntegrationFact]
    public async Task CreateNavMenu_Request_Unauthorized()
    {
        _ = nameof(MarsWebApiClient.NavMenu.Create);
        await _createTest.ValidRequest_Unauthorized();
    }

    [IntegrationFact]
    public async Task CreateNavMenu_ValidRequest_ShouldSuccess()
    {
        await _createTest.ValidRequest_ShouldSuccess();
    }

    [IntegrationFact]
    public void CreateNavMenu_InvalidModelRequest_ValidateError()
    {
        _createTest.InvalidModelRequest_ValidateError(req => req with { Slug = string.Empty }, "Slug");
    }

}
