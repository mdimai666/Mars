using AutoFixture;
using Mars.Core.Exceptions;
using Mars.Integration.Tests.Extensions;
using Mars.Shared.Common;
using Mars.WebApiClient.Interfaces;
using FluentAssertions;

namespace Mars.WebApiClient.Integration.Tests.GeneralTestAbstractions;

public class GeneralGetTests<TEntity, TRequestListQuery, TRequestTableQuery, TResponseSingle, TResponseListItem>
    where TEntity : class
{
    private readonly BaseWebApiClientTests _base;
    private readonly Func<IMarsWebApiClient, Guid, Task<TResponseSingle?>> _getDetailAction;
    private readonly Func<IMarsWebApiClient, TRequestListQuery, Task<ListDataResult<TResponseListItem>>> _listItemsAction;
    private readonly Func<IMarsWebApiClient, TRequestTableQuery, Task<PagingResult<TResponseListItem>>>? _listTableItemsAction;

    private IFixture _fixture => _base._fixture;
    private IMarsWebApiClient GetWebApiClient(bool isAnonymous = false) => _base.GetWebApiClient(isAnonymous);

    public GeneralGetTests(
        BaseWebApiClientTests baseWebApiClient,
        Func<IMarsWebApiClient, Guid, Task<TResponseSingle?>> getDetailAction,
        Func<IMarsWebApiClient, TRequestListQuery, Task<ListDataResult<TResponseListItem>>> listItemsAction,
        Func<IMarsWebApiClient, TRequestTableQuery, Task<PagingResult<TResponseListItem>>>? listTableItemsAction)
    {
        _base = baseWebApiClient;
        _getDetailAction = getDetailAction;
        _listItemsAction = listItemsAction;
        _listTableItemsAction = listTableItemsAction;
    }

    public async Task GetDetail_ValidRequest_ShouldSuccess()
    {
        //Arrange
        var client = GetWebApiClient();
        var entity = await _base.CreateEntity<TEntity>();
        var exp = ReflectionHelper.GetIdPropertyExpression<TEntity>();
        var existId = exp.Compile()(entity);

        //Act
        var item = await _getDetailAction(client, existId);

        //Assert
        item.Should().NotBeNull();
        var expResponseId = ReflectionHelper.GetIdPropertyExpression<TResponseSingle>();
        var responseId = expResponseId.Compile()(item!);
        responseId.Should().Be(existId);
    }

    public async Task GetDetail_Request_Unauthorized()
    {
        //Arrange
        var client = GetWebApiClient(true);

        //Act
        var action = () => _getDetailAction(client, Guid.NewGuid());

        //Assert
        await action.Should().ThrowAsync<UnauthorizedException>();

    }

    public void GetDetail_NotExistEntity_Fail404ShouldReturnNullInsteadException()
    {
        //Arrange
        var client = GetWebApiClient();
        var notExistId = Guid.NewGuid();

        //Act
        var action = () => _getDetailAction(client, notExistId);

        //Assert
        action.Should().NotThrowAsync("Get 404 raise exception but didnt expect").RunSync()
            .Subject.Should().BeNull();
    }

    public async Task List_ValidRequest_ShouldSuccess(TRequestListQuery query, TRequestTableQuery queryTable)
    {
        //Arrange
        var client = GetWebApiClient();
        var items = await _base.CreateManyEntities<TEntity>();
        //Act
        var list = await _listItemsAction(client, query);
        var listTable = await _listTableItemsAction?.Invoke(client, queryTable);

        //Assert
        list.Items.Count.Should().BeGreaterThanOrEqualTo(items.Count);
        if (_listItemsAction is not null)
            listTable.Items.Count.Should().BeGreaterThanOrEqualTo(items.Count);
    }

    public async Task List_Request_Unauthorized(TRequestListQuery query)
    {
        //Arrange
        var client = GetWebApiClient(true);
        //Act
        var action = () => _listItemsAction(client, query);
        var actionTable = () => _listItemsAction?.Invoke(client, query);

        //Assert
        await action.Should().ThrowAsync<UnauthorizedException>();
        if (_listItemsAction is not null)
            await actionTable.Should().ThrowAsync<UnauthorizedException>();
    }
}
