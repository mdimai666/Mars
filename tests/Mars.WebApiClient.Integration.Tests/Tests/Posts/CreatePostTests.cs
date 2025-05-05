using Mars.Host.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Contracts.Posts;
using Mars.Test.Common.FixtureCustomizes;
using Mars.WebApiClient.Integration.Tests.GeneralTestAbstractions;

namespace Mars.WebApiClient.Integration.Tests.Tests.Posts;

public class CreatePostTests : BaseWebApiClientTests
{
    GeneralCreateTests<PostEntity, CreatePostRequest, PostDetailResponse> _createTest;

    public CreatePostTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());

        _createTest = new(this, (client, req) => client.Post.Create(req));
    }

    [IntegrationFact]
    public async Task CreatePost_Request_Unauthorized()
    {
        await _createTest.ValidRequest_Unauthorized();
    }


    [IntegrationFact]
    public async Task CreatePost_ValidRequest_ShouldSuccess()
    {
        await _createTest.ValidRequest_ShouldSuccess();
    }

    [IntegrationFact]
    public void CreatePost_InvalidModelRequest_ValidateError()
    {
        _createTest.InvalidModelRequest_ValidateError(req => req with { Title = string.Empty }, "Title");
    }

}
