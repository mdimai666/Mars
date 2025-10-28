using System.Net.Http.Json;
using System.Text.Json;

namespace ExternalServices.Integration.Tests.KeycloakTests;

public class SsoIntegrationTests : IClassFixture<KeycloakTestContainerFixture>
{
    private readonly KeycloakTestContainerFixture _fixture;

    public SsoIntegrationTests(KeycloakTestContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAccessToken_FromKeycloak_ShouldSuccess()
    {
        using var http = new HttpClient();
        var response = await http.PostAsync(
            $"{_fixture.BaseUrl}/realms/{KeycloakTestContainerFixture.RealmName}/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = KeycloakTestContainerFixture.ClientId,
                ["client_secret"] = KeycloakTestContainerFixture.ClientSecret,
                ["username"] = KeycloakTestContainerFixture.Username,
                ["password"] = KeycloakTestContainerFixture.Password,
            }));

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(response.IsSuccessStatusCode);
        Assert.NotNull(json.GetProperty("access_token").GetString());
    }
}
