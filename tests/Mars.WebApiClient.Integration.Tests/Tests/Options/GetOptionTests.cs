using FluentAssertions;
using Mars.Cms.Abstractions.Dto.Posts;
using Mars.Core.Exceptions;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Server.Contracts.Options;
using Mars.Test.Common.FixtureCustomizes;

namespace Mars.WebApiClient.Integration.Tests.Tests.Options;

public class GetOptionTests : BaseWebApiClientTests
{
    public GetOptionTests(ApplicationFixture appFixture) : base(appFixture)
    {
    }

    [IntegrationFact]
    public async Task GetOption_Request_Unauthorized()
    {
        //Arrange
        var client = GetWebApiClient(true);

        //Act
        var action = () => client.Option.GetOption<ApiOption>();

        //Assert
        await action.Should().ThrowAsync<UnauthorizedException>();
    }

    [IntegrationFact]
    public async Task GetOption_ValidRequest_Succeeds()
    {
        //Arrange
        var client = GetWebApiClient();

        //Act
        var result = await client.Option.GetOption<ApiOption>();

        //Assert
        result.Should().NotBeNull();
    }

    [IntegrationFact]
    public void GetOption_NotExistEntity_Fails404ReturnsNull()
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
    public async Task GetSiteSettings_RequestAnonim_Succeeds()
    {
        //Arrange
        var client = GetWebApiClient(true);

        //Act
        var action = () => client.Option.GetSiteSettings();

        //Assert
        await action.Should().NotThrowAsync<UnauthorizedException>();
    }
}
