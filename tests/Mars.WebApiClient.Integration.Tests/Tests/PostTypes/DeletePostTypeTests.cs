using FluentAssertions;
using Mars.Core.Exceptions;
using Mars.Host.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Test.Common.FixtureCustomizes;

namespace Mars.WebApiClient.Integration.Tests.Tests.PostTypes;

public sealed class DeletePostTypeTests : BaseWebApiClientTests
{
    public DeletePostTypeTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task DeletePostType_Request_Unauthorized()
    {
        //Arrange
        var client = GetWebApiClient(true);
        var entity = await CreateEntity<PostTypeEntity>();

        //Act
        var action = () => client.PostType.Delete(entity.Id);

        //Assert
        await action.Should().ThrowAsync<UnauthorizedException>();
    }

    [IntegrationFact]
    public async Task DeletePostType_ValidRequest_ShouldSuccess()
    {
        //Arrange
        var client = GetWebApiClient();
        var entity = await CreateEntity<PostTypeEntity>();

        //Act
        await client.PostType.Delete(entity.Id);

        //Assert
        AppFixture.MarsDbContext().PostTypes.FirstOrDefault(s => s.Id == entity.Id).Should().BeNull();
    }

    [IntegrationFact]
    public void DeletePostType_NotExistEntity_ThrowNotFoundException()
    {
        //Arrange
        var client = GetWebApiClient();

        //Act
        var action = () => client.PostType.Delete(Guid.NewGuid());

        //Assert
        action.Should().ThrowAsync<NotFoundException>().RunSync();
    }

    [IntegrationFact]
    public async Task DeleteManyPostType_ValidRequest_ShouldSuccess()
    {
        //Arrange
        var client = GetWebApiClient();
        var entityList = await CreateManyEntities<PostTypeEntity>();
        var ids = entityList.Select(s => s.Id).ToArray();

        //Act
        await client.PostType.DeleteMany(ids);

        //Assert
        AppFixture.MarsDbContext().PostTypes.Any(s => ids.Contains(s.Id)).Should().BeFalse();
    }
}
