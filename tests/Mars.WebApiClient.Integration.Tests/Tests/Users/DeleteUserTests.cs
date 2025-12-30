using Mars.Host.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Test.Common.FixtureCustomizes;
using Mars.WebApiClient.Integration.Tests.GeneralTestAbstractions;

namespace Mars.WebApiClient.Integration.Tests.Tests.Users;

public sealed class DeleteUserTests : BaseWebApiClientTests
{
    GeneralDeleteTests<UserEntity, Guid> _deleteTest;

    public DeleteUserTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());

        _deleteTest = new(this, (client, req) => client.User.Delete(req), (client, req) => client.User.DeleteMany(req));
    }

    [IntegrationFact]
    public async Task DeleteUser_Request_Unauthorized()
    {
        await _deleteTest.ValidRequest_Unauthorized();
    }

    [IntegrationFact]
    public async Task DeleteUser_ValidRequest_ShouldSuccess()
    {
        await _deleteTest.ValidRequest_ShouldSuccess();
    }

    [IntegrationFact]
    public async Task DeleteUser_NotExistEntity_ThrowNotFoundException()
    {
        await _deleteTest.NotExistEntity_ThrowNotFoundException();
    }

    [IntegrationFact]
    public async Task DeleteManyUser_ValidRequest_ShouldSuccess()
    {
        await _deleteTest.DeleteMany_ValidRequest_ShouldSuccess();
    }
}
