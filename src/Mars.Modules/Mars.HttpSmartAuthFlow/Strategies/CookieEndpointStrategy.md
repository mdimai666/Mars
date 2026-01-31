# Примеры использования

## 1. Простой эндпоинт с form-data

```csharp
var config = new AuthConfig
{
    Id = "my_api_login",
    Mode = AuthMode.CookieEndpoint,
    Username = "user@example.com",
    Password = "secret123",
    CookieEndpointConfig = new CookieEndpointConfig
    {
        LoginEndpointUrl = "https://api.example.com/auth/login",
        UsernameFieldName = "username",
        PasswordFieldName = "password",
        ContentType = LoginEndpointContentType.FormData
    }
};

var manager = new AuthClientManager();
var client = manager.GetOrCreateClient(config);

var data = await client.Request("https://api.example.com/protected").GetJsonAsync();
```

## 2. Эндпоинт с JSON телом
```csharp
var config = new AuthConfig
{
    Id = "json_api",
    Mode = AuthMode.CookieEndpoint,
    Username = "john.doe",
    Password = "password123",
    CookieEndpointConfig = new CookieEndpointConfig
    {
        LoginEndpointUrl = "https://api.example.com/v1/login",
        UsernameFieldName = "email",
        PasswordFieldName = "password",
        ContentType = LoginEndpointContentType.Json,
        AdditionalFields = new Dictionary<string, string>
        {
            { "rememberMe", "true" },
            { "device", "web-app" }
        },
        LoginHeaders = new Dictionary<string, string>
        {
            { "X-API-Version", "1.0" },
            { "Accept", "application/json" }
        },
        HealthCheckUrl = "https://api.example.com/v1/health",
        AuthCookieName = "session_id"
    }
};
```

## 3. Multipart form-data (редкий случай)
```csharp
var config = new AuthConfig
{
    Id = "multipart_login",
    Mode = AuthMode.CookieEndpoint,
    Username = "admin",
    Password = "admin123",
    CookieEndpointConfig = new CookieEndpointConfig
    {
        LoginEndpointUrl = "https://api.example.com/login",
        ContentType = LoginEndpointContentType.Multipart,
        AdditionalFields = new Dictionary<string, string>
        {
            { "grant_type", "password" }
        }
    }
};
```

## 4. Laravel Sanctum / SPA аутентификация
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
```

## 5. Django REST Framework с сессиями
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
            { "Content-Type", "application/json" },
            { "X-CSRFToken", "..." } // Если нужен CSRF токен
        },
        AuthCookieName = "sessionid",
        HealthCheckUrl = "https://api.example.com/api/auth/me/"
    }
};
```

## 6. Spring Security
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
        FollowRedirects = false // Spring обычно редиректит после логина
    }
};
```
