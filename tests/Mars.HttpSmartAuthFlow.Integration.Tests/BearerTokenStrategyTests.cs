using ExternalServices.TestContainers.Fixtures;
using FluentAssertions;
using Flurl.Http;
using Mars.HttpSmartAuthFlow.Exceptions;
using Mars.HttpSmartAuthFlow.Strategies;
using Microsoft.AspNetCore.Http;
using static ExternalServices.TestContainers.Fixtures.KeycloakTestContainerFixture;

namespace Mars.HttpSmartAuthFlow.Integration.Tests;

public class BearerTokenStrategyTests : IClassFixture<KeycloakTestContainerFixture>
{
    private readonly KeycloakTestContainerFixture _keycloak;

    public BearerTokenStrategyTests(KeycloakTestContainerFixture keycloak)
    {
        _keycloak = keycloak;
    }

    [Fact]
    public async Task AuthRequest_ValidData_Success()
    {
        //Arrange
        using var authManager = new AuthClientManager();

        _ = nameof(BearerTokenStrategy.ApplyAuthenticationAsync);
        var config1 = new AuthConfig
        {
            Id = "keycloak-config1",
            Mode = AuthMode.BearerToken,
            TokenUrl = _keycloak.TokenEndpoint,
            Username = Username,
            Password = Password,
            ClientId = ClientId,
            ClientSecret = ClientSecret,
            Scope = "openid email"
        };

        var url = $"{_keycloak.BaseUrl}/realms/{RealmName}/protocol/openid-connect/userinfo";

        var client1 = authManager.GetOrCreateClient(config1);

        //Act
        var data1 = () => client1.Request(url).GetAsync();

        //Assert
        var res = await data1.Should().NotThrowAsync();
        res.Subject.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task AuthRequest_InvalidData_FailWithAuthenticationException()
    {
        //Arrange
        using var authManager = new AuthClientManager();

        _ = nameof(BearerTokenStrategy.ApplyAuthenticationAsync);
        var config1 = new AuthConfig
        {
            Id = "keycloak-config1",
            Mode = AuthMode.BearerToken,
            TokenUrl = _keycloak.TokenEndpoint,
            Username = Username,
            Password = "invalid_passwrod",
            ClientId = ClientId,
            ClientSecret = ClientSecret,
            Scope = "openid email"
        };

        var url = $"{_keycloak.BaseUrl}/realms/{RealmName}/protocol/openid-connect/userinfo";

        var client1 = authManager.GetOrCreateClient(config1);

        //Act
        var data1 = () => client1.Request(url).GetAsync();

        //Assert
        (await data1.Should().ThrowAsync<FlurlHttpException>())
            .WithInnerException<AuthenticationException>();
    }
}
