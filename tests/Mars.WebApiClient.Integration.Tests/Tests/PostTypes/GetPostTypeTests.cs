using Mars.Host.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Shared.Contracts.MetaFields;
using Mars.Test.Common.FixtureCustomizes;
using FluentAssertions;

namespace Mars.WebApiClient.Integration.Tests.Tests.PostTypes;

public class GetPostTypeTests : BaseWebApiClientTests
{
    public GetPostTypeTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async void GetPostType_ValidRequest_ShouldSuccess()
    {
        //Arrange
        var client = GetWebApiClient();
        var existId = AppFixture.DbFixture.DbContext.PostTypes.First().Id;

        //Act
        var postType = await client.PostType.Get(existId);

        //Assert
        postType.Id.Should().Be(existId);
    }

    [IntegrationFact]
    public void GetPostType_NotExistEntity_Fail404ShouldReturnNullInsteadException()
    {
        //Arrange
        var client = GetWebApiClient();
        var notExistId = Guid.NewGuid();

        //Act
        var action = () => client.PostType.Get(notExistId);

        //Assert
        action.Should().NotThrowAsync("Get 404 raise exception but didnt expect").RunSync()
            .Subject.Should().BeNull();
    }


    [IntegrationFact]
    public async void ListPostType_ValidRequest_ShouldSuccess()
    {
        //Arrange
        var client = GetWebApiClient();
        _ = await CreateManyEntities<PostTypeEntity>();
        //Act
        var list = await client.PostType.List(new());

        //Assert
        list.Items.Count.Should().BeGreaterThanOrEqualTo(3);
    }

    [IntegrationFact]
    public async void GetEditModel_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(MarsWebApiClient.PostType.GetEditModel);
        var client = GetWebApiClient();
        var entity = await CreateEntity<PostTypeEntity>();

        //Act
        var result = await client.PostType.GetEditModel(entity.Id);

        //Assert
        result.Should().NotBeNull();
    }

    [IntegrationFact]
    public async void AllMetaRelationsStructure_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(MarsWebApiClient.PostType.AllMetaRelationsStructure);
        var client = GetWebApiClient();

        //Act
        var result = await client.PostType.AllMetaRelationsStructure();

        //Assert
        result.Should().NotBeNull();
    }

    [IntegrationFact]
    public async void ListMetaValueRelationModels_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(MarsWebApiClient.PostType.ListMetaValueRelationModels);
        var client = GetWebApiClient();
        var query = new MetaValueRelationModelsListQueryRequest() { ModelName = "Post" };

        //Act
        var result = await client.PostType.ListMetaValueRelationModels(query);

        //Assert
        result.Should().NotBeNull();
    }

    [IntegrationFact]
    public async void GetMetaValueRelationModels_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(MarsWebApiClient.PostType.GetMetaValueRelationModels);
        var client = GetWebApiClient();

        //Act
        var result = await client.PostType.GetMetaValueRelationModels("Post", []);

        //Assert
        result.Should().NotBeNull();
    }
}
