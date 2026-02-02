using ExternalServices.TestContainers.Fixtures;
using Mars.Integration.Tests;
using Mars.Integration.Tests.Common;
using Xunit;

namespace ExternalServices.Integration.Tests.KeycloakTests;

[Collection("KeycloakTestsApp")]
public abstract class KeycloakIntegrationTestBase : ApplicationTests//, IClassFixture<KeycloakTestContainerFixture>
{
    public readonly KeycloakTestContainerFixture _keycloak;

    protected KeycloakIntegrationTestBase(ApplicationFixture appFixture, KeycloakTestContainerFixture keycloak) : base(appFixture)
    {
        _keycloak = keycloak;
    }
}

[CollectionDefinition("KeycloakTestsApp")]
public class KeycloakTestsAppCollection : ICollectionFixture<ApplicationFixture>, ICollectionFixture<KeycloakTestContainerFixture>
{
}
