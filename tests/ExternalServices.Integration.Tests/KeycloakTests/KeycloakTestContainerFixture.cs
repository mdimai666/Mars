using System.Net.Http.Json;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Duende.IdentityServer.Test;

namespace ExternalServices.Integration.Tests.KeycloakTests;

public class KeycloakTestContainerFixture : IAsyncLifetime
{
    public const string RealmName = "test-realm";
    public const string ClientId = "test-client";
    public const string ClientSecret = "secret";
    public const string Username = "testuser";
    public const string Password = "password";
    public const string UserEmail = Username + "@example.com";

    public string AuthEndpoint => $"{BaseUrl}/realms/{RealmName}/protocol/openid-connect/auth";
    public string TokenEndpoint => $"{BaseUrl}/realms/{RealmName}/protocol/openid-connect/token";
    public string Issuer => $"{BaseUrl}/realms/{RealmName}";

    private IContainer _container = default!;
    public string BaseUrl { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        var password = "admin";
        var user = "admin";

        _container = new ContainerBuilder("quay.io/keycloak/keycloak:26.0")
            .WithName("test-keycloak")
            .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", user)
            .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", password)
            .WithCommand("start-dev")
            .WithPortBinding(8080, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Running the server"))
            .Build();

        await _container.StartAsync();

        var mappedPort = _container.GetMappedPublicPort(8080);
        BaseUrl = $"http://localhost:{mappedPort}";

        await SetupRealmAsync(BaseUrl, user, password);
    }

    private async Task SetupRealmAsync(string baseUrl, string adminUser, string adminPass)
    {
        using var http = new HttpClient();

        // 1️⃣ Получаем access token администратора
        var tokenResponse = await http.PostAsync($"{baseUrl}/realms/master/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = "admin-cli",
                ["username"] = adminUser,
                ["password"] = adminPass
            }));

        var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = tokenJson.GetProperty("access_token").GetString();

        http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // 2️⃣ Создаём тестовый realm
        var realm = new
        {
            realm = RealmName,
            enabled = true
        };

        await http.PostAsJsonAsync($"{baseUrl}/admin/realms", realm);

        // 3️⃣ Создаём клиента
        var client = new
        {
            clientId = ClientId,
            secret = ClientSecret,
            directAccessGrantsEnabled = true,
            publicClient = false,
            serviceAccountsEnabled = true,
            redirectUris = new[] { "*" }
        };

        await http.PostAsJsonAsync($"{baseUrl}/admin/realms/{RealmName}/clients", client);

        // 4️⃣ Создаём пользователя
        var user = new
        {
            username = Username,
            enabled = true,
            email = UserEmail,
            emailVerified = true,
            firstName = "User",
            lastName = "Test",
            requiredActions = new string[] { },
            credentials = new[]
            {
                new { type = "password", value = Password, temporary = false }
            }
        };

        var createUserResponse = await http.PostAsJsonAsync($"{baseUrl}/admin/realms/{RealmName}/users", user);

        var userId = createUserResponse.Headers.Location!.Segments.Last();

        // 5️⃣ Создаём роль "admin"
        var role = new
        {
            name = "admin",
            description = "Administrator role",
            composite = false,
            clientRole = false,
            attributes = new { }
        };
        await http.PostAsJsonAsync($"{baseUrl}/admin/realms/{RealmName}/roles", role);

        // 6️⃣ Получаем roleId созданной роли
        var roleResponse = await http.GetFromJsonAsync<JsonElement>($"{baseUrl}/admin/realms/{RealmName}/roles/admin");
        var roleId = roleResponse.GetProperty("id").GetString();

        // 7️⃣ Назначаем роль пользователю
        var roleMapping = new[]
        {
            new { id = roleId, name = "admin" }
        };
        await http.PostAsJsonAsync($"{baseUrl}/admin/realms/{RealmName}/users/{userId}/role-mappings/realm", roleMapping);

    }

    public async Task DisposeAsync()
    {
        await _container.StopAsync();
        await _container.DisposeAsync();
    }
}
