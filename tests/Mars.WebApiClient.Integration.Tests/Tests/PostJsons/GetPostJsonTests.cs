using FluentAssertions;
using Mars.Cms.Contracts.PostJsons;
using Mars.Cms.Contracts.Posts;
using Mars.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Test.Common.FixtureCustomizes;
using Mars.WebApiClient.Integration.Tests.GeneralTestAbstractions;
using Microsoft.EntityFrameworkCore;

namespace Mars.WebApiClient.Integration.Tests.Tests.PostJsons;

public class GetPostJsonTests : BaseWebApiClientTests
{
    GeneralGetTests<PostEntity, ListPostQueryRequest, TablePostQueryRequest, PostJsonResponse, PostJsonResponse> _getTest;

    public GetPostJsonTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());

        _getTest = new(
            this,
            (client, id) => client.PostJson.Get(id),
            (client, query) => client.PostJson.List(query, "post"),
            (client, query) => client.PostJson.ListTable(query, "post")
            );

    }

    [IntegrationFact]
    public async void GetPostJson_ValidRequest_Succeeds()
    {
        _ = nameof(MarsWebApiClient.PostJson.Get);
        await _getTest.GetDetail_ValidRequest_ShouldSuccess();
    }

    [IntegrationFact]
    public async void GetPostJsonBySlug_ValidRequest_Succeeds()
    {
        //Arrange
        _ = nameof(MarsWebApiClient.PostJson.GetBySlug);
        var client = GetWebApiClient();
        var exist = AppFixture.DbFixture.DbContext.Posts.AsNoTracking().Include(s => s.PostType).First();

        //Act
        var post = await client.PostJson.GetBySlug(exist.Slug, exist.PostType.TypeName);

        //Assert
        post.Id.Should().Be(exist.Id);
    }

    [IntegrationFact]
    public void GetPostJson_NotExistEntity_Fails404ReturnsNull()
    {
        _getTest.GetDetail_NotExistEntity_Fail404ShouldReturnNullInsteadException();
    }

    [IntegrationFact]
    public async void ListPostJson_ValidRequest_Succeeds()
    {
        await _getTest.List_ValidRequest_ShouldSuccess(new(), new());
    }
}
