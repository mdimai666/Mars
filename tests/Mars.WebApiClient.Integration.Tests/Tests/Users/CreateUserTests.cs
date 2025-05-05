using Mars.Host.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Contracts.Users;
using Mars.Test.Common.FixtureCustomizes;
using Mars.WebApiClient.Integration.Tests.GeneralTestAbstractions;

namespace Mars.WebApiClient.Integration.Tests.Tests.Users;

public class CreateUserTests : BaseWebApiClientTests
{
    GeneralCreateTests<UserEntity, CreateUserRequest, UserDetailResponse> _createTest;

    public CreateUserTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());

        _createTest = new(this, (client, req) => client.User.Create(req));
    }

    [IntegrationFact]
    public async Task CreateUser_Request_Unauthorized()
    {
        await _createTest.ValidRequest_Unauthorized();
    }

    [IntegrationFact]
    public async Task CreateUser_ValidRequest_ShouldSuccess()
    {
        await _createTest.ValidRequest_ShouldSuccess();
    }

    [IntegrationFact]
    public void CreateUser_InvalidModelRequest_ValidateError()
    {
        _createTest.InvalidModelRequest_ValidateError(req => req with { FirstName = string.Empty }, "FirstName");
    }

}
