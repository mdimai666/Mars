```csharp
// Создаем менеджер (один на приложение)
var authManager = new AuthClientManager();

// Конфигурация пользователя 1
var config1 = new AuthConfig
{
    Id = "user1_keycloak",
    Mode = AuthMode.BearerToken,
    TokenUrl = "https://keycloak.example.com/realms/myrealm/protocol/openid-connect/token",
    Username = "user1",
    Password = "pass1",
    ClientId = "my-client",
    ClientSecret = "SECRET"
};

// Конфигурация пользователя 2
var config2 = new AuthConfig
{
    Id = "user2_login_page",
    Mode = AuthMode.CookieForm,
    LoginPageUrl = "https://example.com/login",
    Username = "admin",
    Password = "admin123"
};

// Создаем клиенты (кэшируются по Id)
var client1 = authManager.GetOrCreateClient(config1);
var client2 = authManager.GetOrCreateClient(config2);

// Используем
var data1 = await client1.Request("https://api.example.com/data").GetStringAsync();
var data2 = await client2.Request("https://example.com/protected").GetStringAsync();

// Параллельные запросы с одним конфигом - не будет дублирования авторизации!
var tasks = new List<Task>();
for (int i = 0; i < 10; i++)
{
    tasks.Add(client1.Request("https://api.example.com/data").GetStringAsync());
}
await Task.WhenAll(tasks); // Все запросы поделят одну авторизацию

// Инвалидация клиента
authManager.InvalidateClient("user1_keycloak");

// Статистика
Console.WriteLine($"Active clients: {authManager.GetActiveClientCount()}");

// Очистка при завершении
authManager.Dispose();
```
