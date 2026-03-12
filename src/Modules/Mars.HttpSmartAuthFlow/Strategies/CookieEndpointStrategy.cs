using System.Net;
using System.Text;
using System.Text.Json;
using Mars.HttpSmartAuthFlow.Exceptions;

namespace Mars.HttpSmartAuthFlow.Strategies;

/// <summary>
/// Стратегия аутентификации через эндпоинт с куками
/// Отправляет логин/пароль на известный эндпоинт и сохраняет полученные куки
/// </summary>
public class CookieEndpointStrategy : AuthStrategyBase
{
    private readonly CookieContainer _cookieContainer = new();
    private readonly HttpClient _httpClient;
    private readonly CookieEndpointConfig _endpointConfig;

    public CookieEndpointStrategy(AuthConfig config) : base(config)
    {
        if (string.IsNullOrEmpty(config.Username))
            throw new InvalidOperationException("Username is required for CookieEndpoint mode");
        if (string.IsNullOrEmpty(config.Password))
            throw new InvalidOperationException("Password is required for CookieEndpoint mode");

        _endpointConfig = config.CookieEndpointConfig
            ?? throw new InvalidOperationException("CookieEndpointConfig is required");

        if (string.IsNullOrEmpty(_endpointConfig.LoginEndpointUrl))
            throw new InvalidOperationException("LoginEndpointUrl is required");

        var handler = new HttpClientHandler
        {
            CookieContainer = _cookieContainer,
            UseCookies = true,
            AllowAutoRedirect = _endpointConfig.FollowRedirects
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(Config.TimeoutSeconds)
        };

        // Добавляем стандартные заголовки
        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    public override async Task ApplyAuthenticationAsync(HttpRequestMessage request)
    {
        await EnsureAuthenticatedAsync();
        // Куки добавляются автоматически через CookieContainer

        if (request.RequestUri != null)
        {
            ApplyCookiesToRequest(request, request.RequestUri);
        }
    }

    public override async Task<bool> HandleUnauthorizedAsync(HttpRequestMessage request)
    {
        await InvalidateAsync();
        await EnsureAuthenticatedAsync();
        if (request.RequestUri != null)
        {
            ApplyCookiesToRequest(request, request.RequestUri);
        }
        return true;
    }

    public override async Task InvalidateAsync()
    {
        await ExecuteWithLockAsync(() =>
        {
            ClearCookies();
            _isAuthenticated = false;
            _lastAuthTime = DateTime.MinValue;
            return Task.CompletedTask;
        });
    }

    private async Task EnsureAuthenticatedAsync()
    {
        if (_isAuthenticated && HasValidAuthCookie())
            return;

        await ExecuteWithLockAsync(async () =>
        {
            // Проверяем еще раз после получения лока
            if (_isAuthenticated && HasValidAuthCookie())
                return;

            await AuthenticateAsync();
        });
    }

    private async Task AuthenticateAsync()
    {
        try
        {
            Console.WriteLine($"[{Config.Id}] Authenticating via endpoint: {_endpointConfig.LoginEndpointUrl}");

            // 1. Подготавливаем данные для отправки
            var loginData = new Dictionary<string, string>
            {
                [_endpointConfig.UsernameFieldName] = Config.Username!,
                [_endpointConfig.PasswordFieldName] = Config.Password!
            };

            // Добавляем дополнительные поля
            if (_endpointConfig.AdditionalFields != null)
            {
                foreach (var field in _endpointConfig.AdditionalFields)
                {
                    loginData[field.Key] = field.Value;
                }
            }

            // 2. Создаем содержимое запроса
            HttpContent content = _endpointConfig.ContentType switch
            {
                LoginEndpointContentType.Json => new StringContent(
                    JsonSerializer.Serialize(loginData),
                    Encoding.UTF8,
                    "application/json"
                ),
                LoginEndpointContentType.Multipart => CreateMultipartContent(loginData),
                LoginEndpointContentType.FormData or _ => new FormUrlEncodedContent(loginData)
            };

            // 3. Создаем и отправляем запрос
            var request = new HttpRequestMessage(HttpMethod.Post, _endpointConfig.LoginEndpointUrl)
            {
                Content = content
            };

            // Добавляем кастомные заголовки
            if (_endpointConfig.LoginHeaders != null)
            {
                foreach (var header in _endpointConfig.LoginHeaders)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // Добавляем Referer если нужно
            if (Uri.TryCreate(_endpointConfig.LoginEndpointUrl, UriKind.Absolute, out var uri))
            {
                request.Headers.Referrer = uri;
            }

            // 4. Отправляем запрос
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new AuthenticationException(
                    $"Login failed with status {(int)response.StatusCode}: {errorContent}");
            }

            // 5. Проверяем, получили ли мы куки
            var cookies = _cookieContainer.GetCookies(new Uri(_endpointConfig.LoginEndpointUrl));

            if (cookies.Count == 0)
            {
                // Проверяем, может сервер вернул токен в теле ответа?
                var responseContent = await response.Content.ReadAsStringAsync();

                // Попытка распарсить как токен (если сервер так делает)
                if (!string.IsNullOrWhiteSpace(responseContent))
                {
                    Console.WriteLine($"[{Config.Id}] No cookies received, but response contains data. " +
                                    $"Consider using BearerToken strategy instead.");
                }

                throw new AuthenticationException("Login successful but no cookies received");
            }

            // 6. Валидируем куки
            if (!HasValidAuthCookie())
            {
                Console.WriteLine($"[{Config.Id}] Warning: No authentication cookie found, but received {cookies.Count} cookies");
            }

            _isAuthenticated = true;
            _lastAuthTime = DateTime.UtcNow;

            Console.WriteLine($"[{Config.Id}] Authentication successful. Received {cookies.Count} cookies:");
            foreach (Cookie cookie in cookies)
            {
                var expiresInfo = cookie.Expires != DateTime.MinValue
                    ? $" expires: {cookie.Expires:yyyy-MM-dd HH:mm:ss}"
                    : " (session cookie)";

                Console.WriteLine($"[{Config.Id}]   - {cookie.Name} = {TruncateValue(cookie.Value)}{expiresInfo}");
            }

            // 7. Опционально: проверяем аутентификацию через health check
            if (!string.IsNullOrEmpty(_endpointConfig.HealthCheckUrl))
            {
                await VerifyAuthenticationAsync();
            }
        }
        catch (Exception ex)
        {
            ClearCookies();
            _isAuthenticated = false;
            throw new AuthenticationException("Cookie endpoint authentication failed", ex);
        }
    }

    private bool HasValidAuthCookie()
    {
        if (string.IsNullOrEmpty(_endpointConfig.AuthCookieName))
            return _cookieContainer.GetCookieCount(new Uri(_endpointConfig.LoginEndpointUrl)) > 0;

        try
        {
            var uri = new Uri(_endpointConfig.LoginEndpointUrl);
            var cookies = _cookieContainer.GetCookies(uri);

            foreach (Cookie cookie in cookies)
            {
                if (cookie.Name.Equals(_endpointConfig.AuthCookieName, StringComparison.OrdinalIgnoreCase) &&
                    !cookie.Expired &&
                    (cookie.Expires == DateTime.MinValue || cookie.Expires > DateTime.UtcNow))
                {
                    return true;
                }
            }
        }
        catch
        {
            // Игнорируем ошибки парсинга URI
        }

        return false;
    }

    private async Task VerifyAuthenticationAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(_endpointConfig.HealthCheckUrl!);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[{Config.Id}] Health check passed");
            }
            else
            {
                Console.WriteLine($"[{Config.Id}] Health check failed with status {(int)response.StatusCode}");
                _isAuthenticated = false;
                ClearCookies();
                throw new AuthenticationException($"Health check failed: {(int)response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{Config.Id}] Health check error: {ex.Message}");
            throw;
        }
    }

    private void ApplyCookiesToRequest(HttpRequestMessage request, Uri requestUri)
    {
        // Удаляем существующий заголовок куков (чтобы избежать дублирования)
        if (request.Headers.Contains("Cookie"))
        {
            request.Headers.Remove("Cookie");
        }

        // Получаем подходящие куки для данного URI
        var cookies = _cookieContainer.GetCookies(requestUri);

        if (cookies.Count == 0)
            return;

        // Формируем строку куков в формате "name1=value1; name2=value2"
        var cookieStrings = new List<string>();

        foreach (Cookie cookie in cookies)
        {
            // Пропускаем просроченные и пустые куки
            if (cookie.Expired || string.IsNullOrEmpty(cookie.Value))
                continue;

            if (cookie.Expires != DateTime.MinValue && cookie.Expires <= DateTime.UtcNow)
                continue;

            cookieStrings.Add($"{cookie.Name}={cookie.Value}");
        }

        if (cookieStrings.Count > 0)
        {
            var cookieHeader = string.Join("; ", cookieStrings);
            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);

            Console.WriteLine($"[{Config.Id}] Applied {cookieStrings.Count} cookies to request to {requestUri.Host}");
        }
    }

    private void ClearCookies()
    {
        try
        {
            var uri = new Uri(_endpointConfig.LoginEndpointUrl);
            var cookies = _cookieContainer.GetCookies(uri);

            foreach (Cookie cookie in cookies)
            {
                var expiredCookie = new Cookie(cookie.Name, string.Empty, cookie.Path, cookie.Domain)
                {
                    Expires = DateTime.Now.AddYears(-1)
                };
                _cookieContainer.Add(uri, expiredCookie);
            }
        }
        catch
        {
            // Игнорируем ошибки
        }
    }

    private MultipartFormDataContent CreateMultipartContent(Dictionary<string, string> data)
    {
        var multipartContent = new MultipartFormDataContent();

        foreach (var item in data)
        {
            multipartContent.Add(new StringContent(item.Value), item.Key);
        }

        return multipartContent;
    }

    private string TruncateValue(string value, int maxLength = 50)
    {
        return value.Length > maxLength
            ? value.Substring(0, maxLength) + "..."
            : value;
    }

    public override void Dispose()
    {
        _httpClient.Dispose();
        _authLock.Dispose();
    }
}

// Расширение для получения количества куков
public static class CookieContainerExtensions
{
    public static int GetCookieCount(this CookieContainer container, Uri uri)
    {
        try
        {
            return container.GetCookies(uri).Count;
        }
        catch
        {
            return 0;
        }
    }
}
