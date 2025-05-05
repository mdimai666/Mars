using AutoFixture;
using Mars.Core.Exceptions;
using Mars.WebApiClient.Interfaces;
using FluentAssertions;

namespace Mars.WebApiClient.Integration.Tests.GeneralTestAbstractions;

public class GeneralDeleteTests<TEntity, TRequest>
    where TEntity : class
{
    private readonly BaseWebApiClientTests _base;
    private readonly Func<IMarsWebApiClient, Guid, Task> _deleteAction;

    private IFixture _fixture => _base._fixture;
    private IMarsWebApiClient GetWebApiClient(bool isAnonymous = false) => _base.GetWebApiClient(isAnonymous);

    public GeneralDeleteTests(BaseWebApiClientTests baseWebApiClient, Func<IMarsWebApiClient, Guid, Task> createAction)
    {
        _base = baseWebApiClient;
        _deleteAction = createAction;
    }

    public async Task ValidRequest_Unauthorized()
    {
        //Arrange
        var client = GetWebApiClient(true);

        //Act
        var action = () => _deleteAction(client, Guid.NewGuid());

        //Assert
        //await action.Should().ThrowAsync<FlurlHttpException>();
        await action.Should().ThrowAsync<UnauthorizedException>();
    }

    public async Task ValidRequest_ShouldSuccess()
    {
        //Arrange
        var client = GetWebApiClient();
        var entity = await _base.CreateEntity<TEntity>();
        var exp = ReflectionHelper.GetIdPropertyExpression<TEntity>();
        var id = exp.Compile()(entity);

        //Act
        var action = () => _deleteAction(client, id);

        //Assert
        await action.Should().NotThrowAsync();
        var dbEntity = await _base.GetEntity<TEntity>(id);
        dbEntity.Should().BeNull();
    }

    public async Task NotExistEntity_ThrowNotFoundException()
    {
        //Arrange
        var client = GetWebApiClient();

        //Act
        var action = () => _deleteAction(client, Guid.NewGuid());

        //Assert
        await action.Should().ThrowAsync<NotFoundException>();
    }
}
