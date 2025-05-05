using Mars.Host.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Test.Common.FixtureCustomizes;
using Mars.WebApiClient.Integration.Tests.GeneralTestAbstractions;

namespace Mars.WebApiClient.Integration.Tests.Tests.Posts;

public sealed class DeletePostTests : BaseWebApiClientTests
{
    GeneralDeleteTests<PostEntity, Guid> _deleteTest;

    public DeletePostTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());

        _deleteTest = new(this, (client, req) => client.Post.Delete(req));
    }

    [IntegrationFact]
    public async Task DeletePost_Request_Unauthorized()
    {
        await _deleteTest.ValidRequest_Unauthorized();
    }

    [IntegrationFact]
    public async Task DeletePost_ValidRequest_ShouldSuccess()
    {
        await _deleteTest.ValidRequest_ShouldSuccess();
    }

    [IntegrationFact]
    public async Task DeletePost_NotExistEntity_ThrowNotFoundException()
    {
        await _deleteTest.NotExistEntity_ThrowNotFoundException();
    }
}
