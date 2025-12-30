using Mars.Host.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Test.Common.FixtureCustomizes;
using Mars.WebApiClient.Integration.Tests.GeneralTestAbstractions;

namespace Mars.WebApiClient.Integration.Tests.Tests.Medias;

public class DeleteMediaTests : BaseWebApiClientTests
{
    GeneralDeleteTests<FileEntity, Guid> _deleteTest;

    public DeleteMediaTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());

        _deleteTest = new(this, (client, req) => client.Media.Delete(req), (client, req) => client.Media.DeleteMany(req));
    }

    [IntegrationFact]
    public async Task DeleteMedia_Request_Unauthorized()
    {
        await _deleteTest.ValidRequest_Unauthorized();
    }

    [IntegrationFact]
    public async Task DeleteMedia_ValidRequest_ShouldSuccess()
    {
        await _deleteTest.ValidRequest_ShouldSuccess();
    }

    [IntegrationFact]
    public async Task DeleteMedia_NotExistEntity_ThrowNotFoundException()
    {
        await _deleteTest.NotExistEntity_ThrowNotFoundException();
    }

    [IntegrationFact]
    public async Task DeleteManyMedia_ValidRequest_ShouldSuccess()
    {
        await _deleteTest.DeleteMany_ValidRequest_ShouldSuccess();
    }
}
