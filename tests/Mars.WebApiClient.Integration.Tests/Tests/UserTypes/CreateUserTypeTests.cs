using AutoFixture;
using Mars.Core.Exceptions;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Shared.Contracts.UserTypes;
using Mars.Test.Common.FixtureCustomizes;
using FluentAssertions;

namespace Mars.WebApiClient.Integration.Tests.Tests.UserTypes;

public sealed class CreateUserTypeTests : BaseWebApiClientTests
{
    public CreateUserTypeTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task CreateUserType_Request_Unauthorized()
    {
        //Arrange
        var client = GetWebApiClient(true);
        var request = _fixture.Create<CreateUserTypeRequest>();

        //Act
        var action = () => client.UserType.Create(request);

        //Assert
        await action.Should().ThrowAsync<UnauthorizedException>();
    }

    [IntegrationFact]
    public async Task CreateUserType_ValidRequest_ShouldSuccess()
    {
        //Arrange
        var client = GetWebApiClient();
        var request = _fixture.Create<CreateUserTypeRequest>();

        //Act
        var result = await client.UserType.Create(request);

        //Assert
        result.Title.Should().Be(request.Title);
        AppFixture.MarsDbContext().UserTypes.FirstOrDefault(s => s.Id == request.Id).Should().NotBeNull();
        AppFixture.MarsDbContext().UserTypes.OrderBy(s => s.Id).Should().NotBeNull();
    }

    [IntegrationFact]
    public void CreateUserType_InvalidModelRequest_ValidateError()
    {
        //Arrange
        var client = GetWebApiClient();
        var request = _fixture.Create<CreateUserTypeRequest>();
        request = request with
        {
            Title = string.Empty
        };

        //Act
        var action = () => client.UserType.Create(request);

        //Assert
        action.Should().ThrowAsync<MarsValidationException>().RunSync()
            .And.Errors.Should().ContainKey("Title");
    }

#if false
    [IntegrationFact]
    public void CreateUserType_InvalidSomeParam_CatchUserActionException() //TODO: to do in the future
    {
        //Arrange
        var client = GetWebApiClient();
        var request = _fixture.Create<CreateUserTypeRequest>();

        //Act
        var action = () => client.UserType.Create(request);

        //Assert
        action.Should().ThrowAsync<UserActionException>().RunSync()
            .Which.Message.Should().Be("zuz");
    }
#endif
}
