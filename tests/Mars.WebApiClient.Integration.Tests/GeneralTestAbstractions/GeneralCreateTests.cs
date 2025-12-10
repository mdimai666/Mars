using AutoFixture;
using FluentAssertions;
using Mars.Core.Exceptions;
using Mars.Integration.Tests.Extensions;
using Mars.Test.Common.Helpers;
using Mars.WebApiClient.Interfaces;

namespace Mars.WebApiClient.Integration.Tests.GeneralTestAbstractions;

public class GeneralCreateTests<TEntity, TRequest, TResponse>
    where TEntity : class
{
    private readonly BaseWebApiClientTests _base;
    private readonly Func<IMarsWebApiClient, TRequest, Task<TResponse>> _createAction;

    private IFixture _fixture => _base._fixture;
    private IMarsWebApiClient GetWebApiClient(bool isAnonymous = false) => _base.GetWebApiClient(isAnonymous);

    public GeneralCreateTests(BaseWebApiClientTests baseWebApiClient, Func<IMarsWebApiClient, TRequest, Task<TResponse>> createAction)
    {
        _base = baseWebApiClient;
        _createAction = createAction;
    }

    public async Task ValidRequest_Unauthorized()
    {
        //Arrange
        var client = GetWebApiClient(true);
        var request = _fixture.Create<TRequest>();

        //Act
        var action = () => _createAction(client, request);

        //Assert
        //await action.Should().ThrowAsync<FlurlHttpException>();
        await action.Should().ThrowAsync<UnauthorizedException>();
    }

    public async Task<TEntity> ValidRequest_ShouldSuccess(Func<TRequest, TRequest>? createRequest = null)
    {
        //Arrange
        var client = GetWebApiClient();
        var request = _fixture.Create<TRequest>();
        if (createRequest != null)
            request = createRequest(request);

        //Act
        var result = await _createAction(client, request);

        //Assert
        TEntity? dbEntity;
        if (result is Guid guid)
        {
            dbEntity = await _base.GetEntity<TEntity>(guid);
        }
        else if (ReflectionHelper.HasGuidIdProperty(result.GetType()))
        {
            var exp = ReflectionHelper.GetIdPropertyExpression<TResponse>();
            var _id = exp.Compile().Invoke(result);
            dbEntity = await _base.GetEntity<TEntity>(_id);
        }
        else
        {
            throw new NotImplementedException();
        }

        dbEntity.Should().NotBeNull();
        return dbEntity!;
    }

    public void InvalidModelRequest_ValidateError(Func<TRequest, TRequest> invalidRequest, string invalidFieldName)
    {
        //Arrange
        var client = GetWebApiClient();
        var request = _fixture.Create<TRequest>();

        //Act
        var action = () => _createAction(client, invalidRequest(request));

        //Assert
        action.Should().ThrowAsync<MarsValidationException>().RunSync()
            .And.Errors.Should().ContainKey(invalidFieldName);
    }

}
