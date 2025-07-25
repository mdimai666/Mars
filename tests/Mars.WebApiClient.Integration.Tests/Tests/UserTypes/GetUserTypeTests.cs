using FluentAssertions;
using Mars.Host.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Shared.Contracts.MetaFields;
using Mars.Test.Common.FixtureCustomizes;

namespace Mars.WebApiClient.Integration.Tests.Tests.UserTypes;

public class GetUserTypeTests : BaseWebApiClientTests
{
    public GetUserTypeTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async void GetUserType_ValidRequest_ShouldSuccess()
    {
        //Arrange
        var client = GetWebApiClient();
        var existId = AppFixture.DbFixture.DbContext.UserTypes.First().Id;

        //Act
        var postType = await client.UserType.Get(existId);

        //Assert
        postType.Id.Should().Be(existId);
    }

    [IntegrationFact]
    public void GetUserType_NotExistEntity_Fail404ShouldReturnNullInsteadException()
    {
        //Arrange
        var client = GetWebApiClient();
        var notExistId = Guid.NewGuid();

        //Act
        var action = () => client.UserType.Get(notExistId);

        //Assert
        action.Should().NotThrowAsync("Get 404 raise exception but didnt expect").RunSync()
            .Subject.Should().BeNull();
    }

    [IntegrationFact]
    public async void ListUserType_ValidRequest_ShouldSuccess()
    {
        //Arrange
        var client = GetWebApiClient();
        _ = await CreateManyEntities<UserTypeEntity>();
        //Act
        var list = await client.UserType.List(new());

        //Assert
        list.Items.Count.Should().BeGreaterThanOrEqualTo(3);
    }

    [IntegrationFact]
    public async void GetEditModel_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(MarsWebApiClient.UserType.GetEditModel);
        var client = GetWebApiClient();
        var entity = await CreateEntity<UserTypeEntity>();

        //Act
        var result = await client.UserType.GetEditModel(entity.Id);

        //Assert
        result.Should().NotBeNull();
    }

    [IntegrationFact]
    public async void AllMetaRelationsStructure_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(MarsWebApiClient.UserType.AllMetaRelationsStructure);
        var client = GetWebApiClient();

        //Act
        var result = await client.UserType.AllMetaRelationsStructure();

        //Assert
        result.Should().NotBeNull();
    }

    [IntegrationFact]
    public async void ListMetaValueRelationModels_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(MarsWebApiClient.UserType.ListMetaValueRelationModels);
        var client = GetWebApiClient();
        var query = new MetaValueRelationModelsListQueryRequest() { ModelName = "User" };

        //Act
        var result = await client.UserType.ListMetaValueRelationModels(query);

        //Assert
        result.Should().NotBeNull();
    }

    [IntegrationFact]
    public async void GetMetaValueRelationModels_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(MarsWebApiClient.UserType.GetMetaValueRelationModels);
        var client = GetWebApiClient();

        //Act
        var result = await client.UserType.GetMetaValueRelationModels("User", []);

        //Assert
        result.Should().NotBeNull();
    }
}
