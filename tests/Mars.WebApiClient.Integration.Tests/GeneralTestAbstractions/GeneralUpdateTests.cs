using AutoFixture;
using Mars.Core.Exceptions;
using Mars.Integration.Tests.Extensions;
using Mars.WebApiClient.Integration.Tests;
using Mars.WebApiClient.Interfaces;
using FluentAssertions;

public class GeneralUpdateTests<TEntity, TRequest, TResponse> : GeneralUpdateTests<TEntity, TRequest>
    where TEntity : class
{
    private new readonly Func<IMarsWebApiClient, TRequest, Task<TResponse>> _updateAction;

    public GeneralUpdateTests(BaseWebApiClientTests baseWebApiClient, Func<IMarsWebApiClient, TRequest, Task<TResponse>> updateAction)
        : base(baseWebApiClient, updateAction)
    {
        _updateAction = updateAction;
    }

    public override async Task ValidRequest_ShouldSuccess(Func<TRequest, TRequest> validRequest)
    {
        //Arrange
        var client = GetWebApiClient();
        var entity = await _base.CreateEntity<TEntity>();
        var exp = ReflectionHelper.GetIdPropertyExpression<TEntity>();
        var existId = exp.Compile()(entity);
        var request = _fixture.Create<TRequest>();

        ReflectionHelper.SetGuidIdProperty(request!, existId);
        request = validRequest(request);

        //Act
        var result = await _updateAction(client, request);

        //Assert
        var dbEntity = await _base.GetEntity<TEntity>(existId);
        dbEntity.Should().BeEquivalentTo(request, options => options
                    .ComparingRecordsByValue()
                    .ComparingByMembers<TRequest>()
                    .ExcludingMissingMembers());
    }
}

public class GeneralUpdateTests<TEntity, TRequest>
    where TEntity : class
{
    protected readonly BaseWebApiClientTests _base;
    protected readonly Func<IMarsWebApiClient, TRequest, Task> _updateAction;

    protected IFixture _fixture => _base._fixture;
    protected IMarsWebApiClient GetWebApiClient(bool isAnonymous = false) => _base.GetWebApiClient(isAnonymous);

    public GeneralUpdateTests(BaseWebApiClientTests baseWebApiClient, Func<IMarsWebApiClient, TRequest, Task> updateAction)
    {
        _base = baseWebApiClient;
        _updateAction = updateAction;
    }

    public async Task ValidRequest_Unauthorized()
    {
        //Arrange
        var client = GetWebApiClient(true);
        var request = _fixture.Create<TRequest>();

        //Act
        var action = () => _updateAction(client, request);

        //Assert
        await action.Should().ThrowAsync<UnauthorizedException>();
    }

    public virtual async Task ValidRequest_ShouldSuccess(Func<TRequest, TRequest> validRequest)
    {
        //Arrange
        var client = GetWebApiClient();
        var entity = await _base.CreateEntity<TEntity>();
        var exp = ReflectionHelper.GetIdPropertyExpression<TEntity>();
        var existId = exp.Compile()(entity);
        var request = _fixture.Create<TRequest>();

        ReflectionHelper.SetGuidIdProperty(request!, existId);
        request = validRequest(request);

        //Act
        var action = () => _updateAction(client, request);

        //Assert
        await action.Should().NotThrowAsync();
    }

    public void InvalidModelRequest_ValidateError(Func<TRequest, TRequest> invalidRequest, string invalidFieldName)
    {
        //Arrange
        var client = GetWebApiClient();
        var request = _fixture.Create<TRequest>();

        //Act
        var action = () => _updateAction(client, invalidRequest(request));

        //Assert
        action.Should().ThrowAsync<MarsValidationException>().RunSync()
            .And.Errors.Should().ContainKey(invalidFieldName);
    }
}
