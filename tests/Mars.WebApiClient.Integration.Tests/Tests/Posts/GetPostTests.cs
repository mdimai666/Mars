using FluentAssertions;
using Mars.Host.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Contracts.Posts;
using Mars.Test.Common.FixtureCustomizes;
using Mars.WebApiClient.Integration.Tests.GeneralTestAbstractions;
using Microsoft.EntityFrameworkCore;

namespace Mars.WebApiClient.Integration.Tests.Tests.Posts;

public class GetPostTests : BaseWebApiClientTests
{
    GeneralGetTests<PostEntity, ListPostQueryRequest, TablePostQueryRequest, PostDetailResponse, PostListItemResponse> _getTest;

    public GetPostTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());

        _getTest = new(
            this,
            (client, id) => client.Post.Get(id),
            (client, query) => client.Post.List(query),
            (client, query) => client.Post.ListTable(query)
            );

    }

    [IntegrationFact]
    public async void GetPost_ValidRequest_ShouldSuccess()
    {
        _ = nameof(MarsWebApiClient.Post.Get);
        await _getTest.GetDetail_ValidRequest_ShouldSuccess();
    }

    [IntegrationFact]
    public async void GetPostBySlug_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(MarsWebApiClient.Post.GetBySlug);
        var client = GetWebApiClient();
        var exist = AppFixture.DbFixture.DbContext.Posts.AsNoTracking().Include(s => s.PostType).First();

        //Act
        var post = await client.Post.GetBySlug(exist.Slug, exist.PostType.TypeName);

        //Assert
        post.Id.Should().Be(exist.Id);
    }

    [IntegrationFact]
    public void GetPost_NotExistEntity_Fail404ShouldReturnNullInsteadException()
    {
        _getTest.GetDetail_NotExistEntity_Fail404ShouldReturnNullInsteadException();
    }

    [IntegrationFact]
    public async void ListPost_ValidRequest_ShouldSuccess()
    {
        await _getTest.List_ValidRequest_ShouldSuccess(new(), new());
    }

    [IntegrationFact]
    public async void GetEditModel_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(MarsWebApiClient.Post.GetEditModel);
        var client = GetWebApiClient();
        var entity = await CreateEntity<PostEntity>();

        //Act
        var result = await client.Post.GetEditModel(entity.Id);

        //Assert
        result.Should().NotBeNull();
    }
}
