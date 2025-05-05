using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.Posts;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Options.Models;
using Mars.Test.Common.FixtureCustomizes;
using FluentAssertions;

namespace Mars.WebApiClient.Integration.Tests.Tests.Options;

public class GetOptionTests : BaseWebApiClientTests
{
    public GetOptionTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async void GetOption_Request_Unauthorized()
    {
        //Arrange
        var client = GetWebApiClient(true);

        //Act
        var action = () => client.Option.GetOption<ApiOption>();

        //Assert
        await action.Should().ThrowAsync<UnauthorizedException>();
    }

    [IntegrationFact]
    public async void GetOption_ValidRequest_ShouldSuccess()
    {
        //Arrange
        var client = GetWebApiClient();

        //Act
        var result = await client.Option.GetOption<ApiOption>();

        //Assert
        result.Should().NotBeNull();
    }

    [IntegrationFact]
    public void GetOption_NotExistEntity_Fail404ShouldReturnNullInsteadException()
    {
        //Arrange
        var client = GetWebApiClient();

        //Act
        var action = () => client.Option.GetOption<UpdatePostQuery>();

        //Assert
        action.Should().NotThrowAsync("Get 404 raise exception but didnt expect").RunSync()
                    .Subject.Should().BeNull();
    }

    [IntegrationFact]
    public async void GetSysOptions_RequestAnonim_ShouldSuccess()
    {
        //Arrange
        var client = GetWebApiClient(true);

        //Act
        var action = () => client.Option.GetSysOptions();

        //Assert
        await action.Should().NotThrowAsync<UnauthorizedException>();
    }
}
