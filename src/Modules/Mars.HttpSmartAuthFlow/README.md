# Mars.HttpSmartAuthFlow - Умное управление аутентификацией для HTTP-клиентов

[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

**Mars.HttpSmartAuthFlow** — это библиотека для автоматического управления аутентификацией в HTTP-запросах. Поддерживает различные методы аутентификации и автоматически перелогинивается при получении ошибки 401 Unauthorized.

## 📋 Содержание

- [Особенности](#-особенности)
- [Быстрый старт](#-быстрый-старт)
- [Поддерживаемые методы аутентификации](#-поддерживаемые-методы-аутентификации)
  - [Bearer Token (OAuth2 / Keycloak)](#bearer-token-oauth2--keycloak)
  - [Cookie Endpoint](#cookie-endpoint)
  - [Cookie Form (HTML форма)](#cookie-form-html-форма)
  - [Basic Authentication](#basic-authentication)
  - [API Key](#api-key)
- [Конфигурация](#-конфигурация)
- [Примеры использования](#-примеры-использования)
- [Расширение](#-расширение)
- [Обработка ошибок](#-обработка-ошибок)
- [Best Practices](#-best-practices)
- [Лицензия](#-лицензия)

## ✨ Особенности

- 🔐 **Автоматическая перелогинизация** при получении 401 Unauthorized
- 🎯 **Поддержка нескольких методов аутентификации** (Bearer Token, Cookies, Basic Auth, API Key)
- 🔄 **Параллельная безопасность** — `SemaphoreSlim` предотвращает дублирование авторизации
- 💾 **Кэширование клиентов** по `Config.Id` для переиспользования
- 🗑️ **Автоматическая очистка** неактивных клиентов
- 📊 **Детальное логирование** всех этапов аутентификации
- 🧩 **Расширяемая архитектура** — легко добавить свою стратегию
- 🚫 **Безопасность** — не добавляет кастомные заголовки, не нарушает сигнатуру запросов

## 🚀 Быстрый старт

```csharp
using Mars.HttpSmartAuthFlow;

// Создаем менеджер (один на приложение)
var authManager = new AuthClientManager();

// Конфигурация аутентификации
var config = new AuthConfig
{
    Id = "my_api_client",  // Уникальный ID для кэширования
    Mode = AuthMode.BearerToken,
    TokenUrl = "https://keycloak.example.com/realms/myrealm/protocol/openid-connect/token",
    Username = "user",
    Password = "password",
    ClientId = "my-client",
    ClientSecret = "SECRET",
    Scope = "openid email"
};

// Получаем или создаем клиента
var client = authManager.GetOrCreateClient(config);

// Используем — аутентификация происходит автоматически!
var data = await client.Request("https://api.example.com/protected-data").GetJsonAsync();

// Освобождаем ресурсы при завершении
authManager.Dispose();
```

## 🔐 Поддерживаемые методы аутентификации

### Bearer Token (OAuth2 / Keycloak)

```csharp
var config = new AuthConfig
{
    Id = "keycloak_client",
    Mode = AuthMode.BearerToken,
    TokenUrl = "https://keycloak.example.com/realms/myrealm/protocol/openid-connect/token",
    Username = "user@example.com",
    Password = "password",
    ClientId = "my-client",
    ClientSecret = "my-secret",
    Scope = "api offline_access"
};
```

**Особенности:**
- Автоматическое обновление токена перед истечением срока
- Поддержка `client_credentials`, `password` grant types
- Кэширование токена для всех параллельных запросов

### Cookie Endpoint

Аутентификация через API эндпоинт с получением куков:

```csharp
var config = new AuthConfig
{
    Id = "cookie_api",
    Mode = AuthMode.CookieEndpoint,
    Username = "user@example.com",
    Password = "password",
    CookieEndpointConfig = new CookieEndpointConfig
    {
        LoginEndpointUrl = "https://api.example.com/auth/login",
        UsernameFieldName = "email",
        PasswordFieldName = "password",
        ContentType = LoginEndpointContentType.Json,
        AdditionalFields = new Dictionary<string, string>
        {
            { "rememberMe", "true" }
        },
        LoginHeaders = new Dictionary<string, string>
        {
            { "X-API-Version", "1.0" }
        },
        AuthCookieName = "session_id",
        HealthCheckUrl = "https://api.example.com/auth/health"
    }
};
```

**Поддерживаемые форматы тела запроса:**
- `FormData` (application/x-www-form-urlencoded) — по умолчанию
- `Json` (application/json)
- `Multipart` (multipart/form-data)

### Cookie Form (HTML форма)

Автоматическое извлечение полей формы с HTML-страницы:

```csharp
var config = new AuthConfig
{
    Id = "legacy_website",
    Mode = AuthMode.CookieForm,
    Username = "admin",
    Password = "admin123",
    LoginPageUrl = "https://example.com/login",
    FollowRedirects = true
};
```

**Особенности:**
- Автоматическое определение полей логина и пароля
- Извлечение скрытых полей (`<input type="hidden">`)
- Поддержка редиректов после логина

### Basic Authentication

```csharp
var config = new AuthConfig
{
    Id = "basic_auth_api",
    Mode = AuthMode.BasicAuth,
    Username = "user",
    Password = "password"
};
```

### API Key

```csharp
var config = new AuthConfig
{
    Id = "api_key_service",
    Mode = AuthMode.ApiKey,
    ApiKey = "your-secret-api-key",
    ApiKeyHeaderName = "X-API-Key"  // По умолчанию: "X-API-Key"
};
```

## ⚙️ Конфигурация

### AuthConfig

```csharp
public class AuthConfig
{
    // Обязательные поля
    public string Id { get; set; }              // Уникальный ID для кэширования
    public AuthMode Mode { get; set; }          // Метод аутентификации
    public string? CustomType { get; set; }     // Для определения кастомных стратегий

    // Общие поля
    public string? Username { get; set; }       // Логин
    public string? Password { get; set; }       // Пароль
    public int TimeoutSeconds { get; set; }     // Таймаут (по умолчанию: 30)
    
    // Bearer Token
    public string? TokenUrl { get; set; }       // URL для получения токена
    public string? ClientId { get; set; }       // Client ID
    public string? ClientSecret { get; set; }   // Client Secret
    public string? Scope { get; set; }          // Scope
    
    // Cookie Form
    public string? LoginPageUrl { get; set; }   // URL страницы логина
    public bool FollowRedirects { get; set; }   // Следовать редиректам
    
    // Cookie Endpoint
    public CookieEndpointConfig? CookieEndpointConfig { get; set; }

    // API Key
    public string? ApiKey { get; set; }
    public string? ApiKeyHeaderName { get; set; } = "X-API-Key";
}
```

### CookieEndpointConfig

```csharp
public class CookieEndpointConfig
{
    public string LoginEndpointUrl { get; set; }           // URL эндпоинта логина
    public string UsernameFieldName { get; set; }          // Имя поля логина (по умолчанию: "username")
    public string PasswordFieldName { get; set; }          // Имя поля пароля (по умолчанию: "password")
    public Dictionary<string, string>? AdditionalFields { get; set; }  // Доп. поля
    public LoginEndpointContentType ContentType { get; set; }          // Формат тела (FormData/Json/Multipart)
    public Dictionary<string, string>? LoginHeaders { get; set; }     // Заголовки для запроса
    public string? HealthCheckUrl { get; set; }            // URL для проверки аутентификации
    public string? AuthCookieName { get; set; }            // Имя куки аутентификации
    public bool FollowRedirects { get; set; }              // Следовать редиректам (по умолчанию: true)
}
```

### AuthFlowHandlerOptions

```csharp
var options = new AuthFlowHandlerOptions
{
    MaxRetryAttempts = 2,  // Максимальное количество попыток перелогина
    
    UnauthorizedStatusCodes = new HashSet<HttpStatusCode>
    {
        HttpStatusCode.Unauthorized,           // 401
        (HttpStatusCode)407                    // 407 Proxy Authentication Required
    },
    
    TreatForbiddenAsUnauthorized = false,  // Считать ли 403 как 401
    
    RetryableExceptions = new HashSet<Type>
    {
        typeof(HttpRequestException),
        //typeof(TaskCanceledException),
        typeof(IOException),
        typeof(AuthenticationException)
    },
    
};
```

## 📚 Примеры использования

### Пример 1: Простое использование

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

// Создаем клиента (кэшируются по Id)
var client1 = authManager.GetOrCreateClient(config1);

// Используем
var data1 = await client1.Request("https://api.example.com/data").GetStringAsync();

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

### Пример 2: Параллельные запросы с одним конфигом

```csharp
var config = new AuthConfig
{
    Id = "shared_client",
    Mode = AuthMode.BearerToken,
    TokenUrl = "https://api.example.com/token",
    Username = "user",
    Password = "pass",
    ClientId = "client"
};

var manager = new AuthClientManager();
var client = manager.GetOrCreateClient(config);

// Запускаем 10 параллельных запросов
var tasks = new List<Task>();
for (int i = 0; i < 10; i++)
{
    tasks.Add(Task.Run(async () =>
    {
        var data = await client.Request($"https://api.example.com/data/{i}").GetJsonAsync();
        Console.WriteLine($"Request {i} completed");
    }));
}

// Все запросы поделят ОДНУ авторизацию благодаря SemaphoreSlim!
await Task.WhenAll(tasks);

Console.WriteLine($"Active clients: {manager.GetActiveClientCount()}");  // = 1
```

### Пример 3: Keycloak с обработкой ошибок

```csharp
var manager = new AuthClientManager();
var config = new AuthConfig
{
    Id = "keycloak_prod",
    Mode = AuthMode.BearerToken,
    TokenUrl = "https://keycloak.example.com/realms/myrealm/protocol/openid-connect/token",
    Username = "service-account",
    Password = "password",
    ClientId = "my-service",
    ClientSecret = "secret",
    TimeoutSeconds = 60
};

try
{
    var client = manager.GetOrCreateClient(config);
    var data = await client.Request("https://api.example.com/protected").GetJsonAsync();
    
    Console.WriteLine("Success!");
}
catch (AuthenticationException ex)
{
    // Ошибка аутентификации
    Console.WriteLine($"Auth failed: {ex.Message}");
    manager.InvalidateClient(config.Id);  // Сбрасываем кэш
}
catch (FlurlHttpException ex) when (ex.StatusCode == 403)
{
    // Доступ запрещен
    Console.WriteLine("Access denied - check permissions");
}
catch (Exception ex)
{
    // Другие ошибки
    Console.WriteLine($"Request failed: {ex.Message}");
}
```

### Пример 4: Laravel Sanctum / SPA аутентификация

```csharp
var config = new AuthConfig
{
    Id = "laravel_spa",
    Mode = AuthMode.CookieEndpoint,
    Username = "user@domain.com",
    Password = "password",
    CookieEndpointConfig = new CookieEndpointConfig
    {
        LoginEndpointUrl = "https://api.example.com/sanctum/token",
        UsernameFieldName = "email",
        PasswordFieldName = "password",
        ContentType = LoginEndpointContentType.FormData,
        AdditionalFields = new Dictionary<string, string>
        {
            { "device_name", "web-app" },
            { "expires_in", "2678400" } // 31 день
        },
        LoginHeaders = new Dictionary<string, string>
        {
            { "Accept", "application/json" },
            { "X-Requested-With", "XMLHttpRequest" }
        },
        AuthCookieName = "laravel_session",
        HealthCheckUrl = "https://api.example.com/api/user"
    }
};

var client = new AuthClientManager().GetOrCreateClient(config);
var user = await client.Request("https://api.example.com/api/user").GetJsonAsync();
```

### Пример 5: Django REST Framework

```csharp
var config = new AuthConfig
{
    Id = "django_api",
    Mode = AuthMode.CookieEndpoint,
    Username = "admin",
    Password = "admin123",
    CookieEndpointConfig = new CookieEndpointConfig
    {
        LoginEndpointUrl = "https://api.example.com/api/auth/login/",
        ContentType = LoginEndpointContentType.Json,
        LoginHeaders = new Dictionary<string, string>
        {
            { "Accept", "application/json" },
            { "Content-Type", "application/json" }
        },
        AuthCookieName = "sessionid",
        HealthCheckUrl = "https://api.example.com/api/auth/me/"
    }
};
```

### Пример 6: Spring Security

```csharp
var config = new AuthConfig
{
    Id = "spring_security",
    Mode = AuthMode.CookieEndpoint,
    Username = "user",
    Password = "password",
    CookieEndpointConfig = new CookieEndpointConfig
    {
        LoginEndpointUrl = "https://api.example.com/login",
        UsernameFieldName = "username",
        PasswordFieldName = "password",
        ContentType = LoginEndpointContentType.FormData,
        AdditionalFields = new Dictionary<string, string>
        {
            { "_spring_security_remember_me", "on" }
        },
        AuthCookieName = "JSESSIONID",
        FollowRedirects = false
    }
};
```

## 🧩 Расширение

### Добавление своей стратегии аутентификации

```csharp
// 1. Создаем новую стратегию
public class CustomJwtStrategy : AuthStrategyBase
{
    private readonly string _jwtToken;
    
    public CustomJwtStrategy(AuthConfig config) : base(config)
    {
        _jwtToken = config.CustomToken ?? throw new InvalidOperationException("CustomToken required");
        _isAuthenticated = true;
    }
    
    public override Task ApplyAuthenticationAsync(HttpRequestMessage request)
    {
        request.Headers.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _jwtToken);
        return Task.CompletedTask;
    }
    
    public override Task<bool> HandleUnauthorizedAsync(HttpRequestMessage request)
    {
        // Наша стратегия не поддерживает перелогин
        return Task.FromResult(false);
    }
    
    public override Task InvalidateAsync()
    {
        _isAuthenticated = false;
        return Task.CompletedTask;
    }
}

// 2. Создаем кастомную фабрику
public class CustomAuthStrategyFactory : AuthStrategyFactory
{
    public override IAuthStrategy Create(AuthConfig config)
    {
        if (config.Mode == AuthMode.BearerToken && !string.IsNullOrEmpty(config.CustomToken))
        {
            return new CustomJwtStrategy(config);
        }
        
        return base.Create(config);
    }
}

// 3. Используем
var manager = new AuthClientManager(new CustomAuthStrategyFactory());
```

### Добавление нового режима аутентификации

```csharp
// 1. Расширяем перечисление
public enum AuthMode
{
    BearerToken,
    CookieForm,
    CookieEndpoint,
    BasicAuth,
    ApiKey,
    CustomJwt  // Новый режим
}

// 2. Создаем стратегию (см. выше)

// 3. Регистрируем в фабрике
public class ExtendedAuthStrategyFactory : AuthStrategyFactory
{
    public override IAuthStrategy Create(AuthConfig config)
    {
        if (config.Mode == AuthMode.CustomJwt)
        {
            return new CustomJwtStrategy(config);
        }
        
        return base.Create(config);
    }
}
```

## ⚠️ Обработка ошибок

### Типичные исключения

| Исключение | Причина | Рекомендация |
|------------|---------|--------------|
| `AuthenticationException` | Ошибка аутентификации (неверные креды, недоступен эндпоинт) | Проверить конфигурацию, сбросить кэш через `InvalidateClient()` |
| `FlurlHttpException` с кодом 401 | Токен/куки недействительны | Обычно обрабатывается автоматически |
| `FlurlHttpException` с кодом 403 | Нет прав доступа | Проверить права пользователя |
| `OperationCanceledException` | Таймаут или отмена запроса | Увеличить `TimeoutSeconds` |
| `HttpRequestException` | Сетевая ошибка | Проверить подключение |

### Обработка с логированием

```csharp
var logger = LoggerFactory.Create(builder => 
    builder.AddConsole()).CreateLogger<Program>();

var manager = new AuthClientManager();

try
{
    var client = manager.GetOrCreateClient(config);
    var result = await client.Request(url).GetJsonAsync();
    
    logger.LogInformation("Request successful");
}
catch (AuthenticationException ex)
{
    logger.LogError(ex, "Authentication failed for config {ConfigId}", config.Id);
    manager.InvalidateClient(config.Id);
}
catch (FlurlHttpException ex) when (ex.Call.Response?.StatusCode == 401)
{
    logger.LogWarning("Unauthorized - retrying...");
    // AuthFlow автоматически повторит запрос
    throw;
}
catch (Exception ex)
{
    logger.LogError(ex, "Unexpected error");
    throw;
}
```

## 💡 Best Practices

### 1. Используйте осмысленные ID для конфигураций

```csharp
// ПЛОХО
var config = new AuthConfig { Id = "1", ... };

// ХОРОШО
var config = new AuthConfig 
{ 
    Id = $"keycloak_{environment}_{clientId}", 
    ...
};
```

### 2. Инвалидируйте клиенты при смене конфигурации

```csharp
public void UpdateCredentials(string configId, string newUsername, string newPassword)
{
    // Сначала инвалидируем старый клиент
    _authManager.InvalidateClient(configId);
    
    // Создаем новый с обновленными данными
    var newConfig = new AuthConfig
    {
        Id = configId,
        Username = newUsername,
        Password = newPassword,
        // ...
    };
    
    var client = _authManager.GetOrCreateClient(newConfig);
}
```

### 3. Ограничьте время жизни клиентов

```csharp
// Создаем таймер для очистки старых клиентов
var cleanupTimer = new Timer(_ =>
{
    _authManager.InvalidateAll();  // или свою логику
}, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
```

### 4. Используйте Health Check для критичных систем

```csharp
var config = new AuthConfig
{
    // ...
    CookieEndpointConfig = new CookieEndpointConfig
    {
        // ...
        HealthCheckUrl = "https://api.example.com/health",
        AuthCookieName = "session_id"
    }
};
```

### 5. Настройте логирование для отладки

```csharp
var options = new AuthFlowHandlerOptions
{
    MaxRetryAttempts = 3,
    TreatForbiddenAsUnauthorized = true
};

// Используйте ILogger для детального логирования
var logger = LoggerFactory.Create(builder => 
    builder.AddConsole().SetMinimumLevel(LogLevel.Debug))
    .CreateLogger<AuthFlowHandler>();

var handler = new AuthFlowHandler(strategy, options, logger);
```

## 📝 Лицензия

Этот проект лицензирован под MIT License - смотрите файл [LICENSE](LICENSE) для подробностей.

---

**Разработано с ❤️ для упрощения работы с аутентификацией в .NET**
