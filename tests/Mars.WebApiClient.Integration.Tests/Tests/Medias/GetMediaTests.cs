using Mars.Host.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Contracts.Files;
using Mars.Test.Common.FixtureCustomizes;
using Mars.WebApiClient.Integration.Tests.GeneralTestAbstractions;

namespace Mars.WebApiClient.Integration.Tests.Tests.Medias;

public class GetMediaTests : BaseWebApiClientTests
{
    GeneralGetTests<FileEntity, ListFileQueryRequest, TableFileQueryRequest, FileDetailResponse, FileListItemResponse> _getTest;

    public GetMediaTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());

        _getTest = new(
            this,
            (client, id) => client.Media.Get(id),
            (client, query) => client.Media.List(query),
            (client, query) => client.Media.ListTable(query)
            );

    }

    [IntegrationFact]
    public async void GetFile_Request_Unauthorized()
    {
        await _getTest.GetDetail_Request_Unauthorized();
    }

    [IntegrationFact]
    public async void GetFile_ValidRequest_ShouldSuccess()
    {
        await _getTest.GetDetail_ValidRequest_ShouldSuccess();
    }

    [IntegrationFact]
    public void GetFile_NotExistEntity_Fail404ShouldReturnNullInsteadException()
    {
        _getTest.GetDetail_NotExistEntity_Fail404ShouldReturnNullInsteadException();
    }

    [IntegrationFact]
    public async void ListFile_Request_Unauthorized()
    {
        await _getTest.List_Request_Unauthorized(new());
    }

    [IntegrationFact]
    public async void ListFile_ValidRequest_ShouldSuccess()
    {
        await _getTest.List_ValidRequest_ShouldSuccess(new(), new());
    }
}
