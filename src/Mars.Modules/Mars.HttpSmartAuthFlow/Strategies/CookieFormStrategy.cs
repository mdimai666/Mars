using System.Net;
using System.Text.RegularExpressions;
using Mars.HttpSmartAuthFlow.Dto;
using Mars.HttpSmartAuthFlow.Exceptions;

namespace Mars.HttpSmartAuthFlow.Strategies;

// Не протестировано еще
public class CookieFormStrategy : AuthStrategyBase
{
    private readonly CookieContainer _cookieContainer = new();
    private readonly HttpClient _httpClient;
    private string? _loginActionUrl;
    private string? _usernameField;
    private string? _passwordField;
    private Dictionary<string, string> _hiddenFields = [];

    public CookieFormStrategy(AuthConfig config) : base(config)
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = _cookieContainer,
            UseCookies = true,
            AllowAutoRedirect = Config.FollowRedirects
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(Config.TimeoutSeconds)
        };

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
            //_cookieContainer.Add(new Uri(Config.LoginPageUrl ?? "http://localhost"),
            //    new Cookie("dummy", "delete") { Expires = DateTime.Now.AddDays(-1) });
            ClearCookies();
            _isAuthenticated = false;
            _loginActionUrl = null;
            return Task.CompletedTask;
        });
    }

    private async Task EnsureAuthenticatedAsync()
    {
        if (_isAuthenticated && HasValidAuthCookie())
            return;

        await ExecuteWithLockAsync(async () =>
        {
            if (_isAuthenticated && HasValidAuthCookie())
                return;

            await AuthenticateAsync();
        });
    }

    private async Task AuthenticateAsync()
    {
        if (string.IsNullOrEmpty(Config.LoginPageUrl))
            throw new InvalidOperationException("LoginPageUrl is required for CookieForm mode");

        try
        {
            // 1. Получаем страницу логина
            Console.WriteLine($"[{Config.Id}] Fetching login page: {Config.LoginPageUrl}");
            var loginPageResponse = await _httpClient.GetAsync(Config.LoginPageUrl);
            var htmlContent = await loginPageResponse.Content.ReadAsStringAsync();

            // 2. Парсим форму
            var formInfo = ParseLoginForm(htmlContent, Config.LoginPageUrl);

            if (formInfo == null)
                throw new AuthenticationException("Could not find login form on the page");

            _loginActionUrl = formInfo.ActionUrl;
            _usernameField = formInfo.UsernameField;
            _passwordField = formInfo.PasswordField;
            _hiddenFields = formInfo.HiddenFields;

            Console.WriteLine($"[{Config.Id}] Found form action: {formInfo.ActionUrl}");
            Console.WriteLine($"[{Config.Id}] Username field: {formInfo.UsernameField}");
            Console.WriteLine($"[{Config.Id}] Password field: {formInfo.PasswordField}");

            // 3. Подготавливаем данные для отправки
            var loginData = new List<KeyValuePair<string, string>>
            {
                new(formInfo.UsernameField, Config.Username!),
                new(formInfo.PasswordField, Config.Password!)
            };

            foreach (var hiddenField in formInfo.HiddenFields)
            {
                loginData.Add(new(hiddenField.Key, hiddenField.Value));
            }

            // 4. Отправляем форму
            Console.WriteLine($"[{Config.Id}] Submitting login form");

            var request = new HttpRequestMessage(HttpMethod.Post, formInfo.ActionUrl)
            {
                Content = new FormUrlEncodedContent(loginData)
            };

            request.Headers.Referrer = new Uri(Config.LoginPageUrl);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new AuthenticationException(
                    $"Login failed: {response.StatusCode}. Response: {errorContent}");
            }

            var cookies = _cookieContainer.GetCookies(new Uri(formInfo.ActionUrl));

            // 5. Валидируем куки
            if (!HasValidAuthCookie())
            {
                Console.WriteLine($"[{Config.Id}] Warning: No authentication cookie found, but received {cookies.Count} cookies");
            }

            _isAuthenticated = true;
            _lastAuthTime = DateTime.UtcNow;

            Console.WriteLine($"[{Config.Id}] Authentication successful. Cookies: {cookies.Count}");

            foreach (Cookie cookie in cookies)
            {
                Console.WriteLine($"[{Config.Id}]   - {cookie.Name} = {cookie.Value}");
            }
        }
        catch (Exception ex)
        {
            ClearCookies();
            _isAuthenticated = false;
            throw new AuthenticationException("Cookie authentication failed", ex);
        }
    }

    private LoginFormInfo? ParseLoginForm(string htmlContent, string loginPageUrl)
    {
        // Простая реализация парсинга без внешних зависимостей
        // Для полноценного парсинга используйте AngleSharp

        var actionMatch = Regex.Match(htmlContent,
            @"<form[^>]+action\s*=\s*[""']([^""']+)[^>]*>",
            RegexOptions.IgnoreCase);

        var actionUrl = actionMatch.Success
            ? actionMatch.Groups[1].Value
            : loginPageUrl;

        if (!Uri.IsWellFormedUriString(actionUrl, UriKind.Absolute))
        {
            actionUrl = new Uri(new Uri(loginPageUrl), actionUrl).ToString();
        }

        var usernameField = FindUsernameField(htmlContent);
        var passwordField = FindPasswordField(htmlContent);

        if (string.IsNullOrEmpty(usernameField) || string.IsNullOrEmpty(passwordField))
            return null;

        var hiddenFields = ParseHiddenFields(htmlContent);

        return new LoginFormInfo
        {
            ActionUrl = actionUrl,
            UsernameField = usernameField,
            PasswordField = passwordField,
            HiddenFields = hiddenFields
        };
    }

    private string? FindUsernameField(string html)
    {
        var usernameKeywords = new[] { "user", "login", "email", "username", "mail" };

        var inputs = Regex.Matches(html,
            @"<input[^>]+name\s*=\s*[""']([^""']+)[^>]+type\s*=\s*[""'](text|email)[^>]*>",
            RegexOptions.IgnoreCase);

        foreach (Match input in inputs)
        {
            var name = input.Groups[1].Value.ToLower();
            if (usernameKeywords.Any(kw => name.Contains(kw)))
            {
                return input.Groups[1].Value;
            }
        }

        // Fallback: первый текстовый инпут
        if (inputs.Count > 0)
            return inputs[0].Groups[1].Value;

        return null;
    }

    private string? FindPasswordField(string html)
    {
        var match = Regex.Match(html,
            @"<input[^>]+name\s*=\s*[""']([^""']+)[^>]+type\s*=\s*[""']password[^>]*>",
            RegexOptions.IgnoreCase);

        return match.Success ? match.Groups[1].Value : null;
    }

    private Dictionary<string, string> ParseHiddenFields(string html)
    {
        var hiddenFields = new Dictionary<string, string>();

        var matches = Regex.Matches(html,
            @"<input[^>]+type\s*=\s*[""']hidden[""'][^>]+name\s*=\s*[""']([^""']+)[^>]+value\s*=\s*[""']([^""']*)",
            RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var name = match.Groups[1].Value;
            var value = match.Groups[2].Value;
            hiddenFields[name] = value;
        }

        return hiddenFields;
    }

    private bool HasValidAuthCookie()
    {
        return _cookieContainer.GetCookieCount(new Uri(_loginActionUrl ?? Config.LoginPageUrl!)) > 0;
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
            var uri = new Uri(_loginActionUrl ?? Config.LoginPageUrl!);
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

    public override void Dispose()
    {
        _httpClient.Dispose();
        _authLock.Dispose();
    }
}
