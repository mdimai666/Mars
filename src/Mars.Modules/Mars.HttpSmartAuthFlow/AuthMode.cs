namespace Mars.HttpSmartAuthFlow;

public enum AuthMode
{
    BearerToken,
    CookieForm,        // Через парсинг HTML формы
    CookieEndpoint,    // Через прямой API эндпоинт
    BasicAuth,
    ApiKey
}
