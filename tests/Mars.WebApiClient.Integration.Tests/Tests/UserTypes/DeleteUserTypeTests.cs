using FluentAssertions;
using Mars.Core.Exceptions;
using Mars.Host.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Test.Common.FixtureCustomizes;

namespace Mars.WebApiClient.Integration.Tests.Tests.UserTypes;

public sealed class DeleteUserTypeTests : BaseWebApiClientTests
{
    public DeleteUserTypeTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task DeleteUserType_Request_Unauthorized()
    {
        //Arrange
        var client = GetWebApiClient(true);
        var entity = await CreateEntity<UserTypeEntity>();

        //Act
        var action = () => client.UserType.Delete(entity.Id);

        //Assert
        await action.Should().ThrowAsync<UnauthorizedException>();
    }

    [IntegrationFact]
    public async Task DeleteUserType_ValidRequest_ShouldSuccess()
    {
        //Arrange
        var client = GetWebApiClient();
        var entity = await CreateEntity<UserTypeEntity>();

        //Act
        await client.UserType.Delete(entity.Id);

        //Assert
        AppFixture.MarsDbContext().UserTypes.FirstOrDefault(s => s.Id == entity.Id).Should().BeNull();
    }

    [IntegrationFact]
    public void DeleteUserType_NotExistEntity_ThrowNotFoundException()
    {
        //Arrange
        var client = GetWebApiClient();

        //Act
        var action = () => client.UserType.Delete(Guid.NewGuid());

        //Assert
        action.Should().ThrowAsync<NotFoundException>().RunSync();
    }

    [IntegrationFact]
    public async Task DeleteManyUserType_ValidRequest_ShouldSuccess()
    {
        //Arrange
        var client = GetWebApiClient();
        var entityList = await CreateManyEntities<UserTypeEntity>();
        var ids = entityList.Select(s => s.Id).ToArray();

        //Act
        await client.UserType.DeleteMany(ids);

        //Assert
        AppFixture.MarsDbContext().UserTypes.Any(s => ids.Contains(s.Id)).Should().BeFalse();
    }
}
