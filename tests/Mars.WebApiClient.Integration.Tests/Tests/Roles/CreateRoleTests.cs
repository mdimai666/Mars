using Mars.Host.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Contracts.Roles;
using Mars.Test.Common.FixtureCustomizes;
using Mars.WebApiClient.Integration.Tests.GeneralTestAbstractions;

namespace Mars.WebApiClient.Integration.Tests.Tests.Roles;

public class CreateRoleTests : BaseWebApiClientTests
{
    GeneralCreateTests<RoleEntity, CreateRoleRequest, RoleDetailResponse> _createTest;

    public CreateRoleTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());

        _createTest = new(this, (client, req) => client.Role.Create(req));
    }

    [IntegrationFact]
    public async Task CreateRole_Request_Unauthorized()
    {
        await _createTest.ValidRequest_Unauthorized();
    }

    [IntegrationFact]
    public async Task CreateRole_ValidRequest_ShouldSuccess()
    {
        await _createTest.ValidRequest_ShouldSuccess();
    }

    [IntegrationFact]
    public void CreateRole_InvalidModelRequest_ValidateError()
    {
        _createTest.InvalidModelRequest_ValidateError(req => req with { Name = string.Empty }, "Name");
    }

}
