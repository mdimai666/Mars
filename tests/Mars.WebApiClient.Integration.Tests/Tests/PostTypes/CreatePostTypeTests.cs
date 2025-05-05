using AutoFixture;
using Mars.Core.Exceptions;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Shared.Contracts.PostTypes;
using Mars.Test.Common.FixtureCustomizes;
using FluentAssertions;

namespace Mars.WebApiClient.Integration.Tests.Tests.PostTypes;


public sealed class CreatePostTypeTests : BaseWebApiClientTests
{
    public CreatePostTypeTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task CreatePostType_Request_Unauthorized()
    {
        //Arrange
        var client = GetWebApiClient(true);
        var request = _fixture.Create<CreatePostTypeRequest>();

        //Act
        var action = () => client.PostType.Create(request);

        //Assert
        await action.Should().ThrowAsync<UnauthorizedException>();
    }

    [IntegrationFact]
    public async Task CreatePostType_ValidRequest_ShouldSuccess()
    {
        //Arrange
        var client = GetWebApiClient();
        var request = _fixture.Create<CreatePostTypeRequest>();

        //Act
        var result = await client.PostType.Create(request);

        //Assert
        result.Title.Should().Be(request.Title);
        AppFixture.MarsDbContext().PostTypes.FirstOrDefault(s => s.Id == request.Id).Should().NotBeNull();
        AppFixture.MarsDbContext().PostTypes.OrderBy(s => s.Id).Should().NotBeNull();
    }

    [IntegrationFact]
    public void CreatePostType_InvalidModelRequest_ValidateError()
    {
        //Arrange
        var client = GetWebApiClient();
        var request = _fixture.Create<CreatePostTypeRequest>();
        request = request with
        {
            Title = string.Empty
        };

        //Act
        var action = () => client.PostType.Create(request);

        //Assert
        action.Should().ThrowAsync<MarsValidationException>().RunSync()
            .And.Errors.Should().ContainKey("Title");
    }

#if false
    [IntegrationFact]
    public void CreatePostType_InvalidSomeParam_CatchUserActionException() //TODO: to do in the future
    {
        //Arrange
        var client = GetWebApiClient();
        var request = _fixture.Create<CreatePostTypeRequest>();

        //Act
        var action = () => client.PostType.Create(request);

        //Assert
        action.Should().ThrowAsync<UserActionException>().RunSync()
            .Which.Message.Should().Be("zuz");
    } 
#endif
}
