namespace Mars.HttpSmartAuthFlow.Strategies;

/// <summary>
/// Конфигурация для стратегии CookieEndpoint
/// </summary>
public class CookieEndpointConfig
{
    /// <summary>
    /// URL эндпоинта логина (например: "https://api.example.com/auth/login")
    /// </summary>
    public string LoginEndpointUrl { get; set; } = string.Empty;

    /// <summary>
    /// Имя поля для логина в запросе (по умолчанию: "username")
    /// </summary>
    public string UsernameFieldName { get; set; } = "username";

    /// <summary>
    /// Имя поля для пароля в запросе (по умолчанию: "password")
    /// </summary>
    public string PasswordFieldName { get; set; } = "password";

    /// <summary>
    /// Дополнительные поля для отправки (например: {"rememberMe": "true"})
    /// </summary>
    public Dictionary<string, string>? AdditionalFields { get; set; }

    /// <summary>
    /// Формат тела запроса (FormData, Json, Multipart)
    /// </summary>
    public LoginEndpointContentType ContentType { get; set; } = LoginEndpointContentType.FormData;

    /// <summary>
    /// Заголовки для запроса логина (например: {"X-API-Version": "1.0"})
    /// </summary>
    public Dictionary<string, string>? LoginHeaders { get; set; }

    /// <summary>
    /// Путь для проверки аутентификации (опционально)
    /// </summary>
    public string? HealthCheckUrl { get; set; }

    /// <summary>
    /// Имя куки, которая указывает на успешную аутентификацию
    /// </summary>
    public string? AuthCookieName { get; set; }

    /// <summary>
    /// Следовать редиректам после логина
    /// </summary>
    public bool FollowRedirects { get; set; } = true;
}

/// <summary>
/// Режим отправки данных для эндпоинта логина
/// </summary>
public enum LoginEndpointContentType
{
    /// <summary>
    /// application/x-www-form-urlencoded (по умолчанию)
    /// </summary>
    FormData,

    /// <summary>
    /// application/json
    /// </summary>
    Json,

    /// <summary>
    /// multipart/form-data
    /// </summary>
    Multipart
}
