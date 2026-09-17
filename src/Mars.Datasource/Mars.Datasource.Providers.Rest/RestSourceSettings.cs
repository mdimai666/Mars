using Mars.Datasource.Contracts.Models;
using Mars.HttpSmartAuthFlow;

namespace Mars.Datasource.Providers.Rest;

/// <summary>Настройки rest-источника из <see cref="DatasourceConfig.Settings"/>.</summary>
public class RestSourceSettings
{
    public const int DefaultTimeoutSec = 100;

    /// <summary>Адрес API: относительно него разрешаются пути операций и адреса из документа запросов.</summary>
    public string BaseUrl { get; init; } = "";

    /// <summary>Как собрать каталог операций: значение <see cref="RestDiscovery"/>.</summary>
    public string Discovery { get; init; } = RestDiscovery.WordPress;

    /// <summary>Адрес описания API; пусто — взять <see cref="RestDiscovery.DefaultUrl"/>.</summary>
    public string DiscoveryUrl { get; init; } = "";

    public int TimeoutSec { get; init; } = DefaultTimeoutSec;

    /// <summary>Доступы источника; null — запросы без аутентификации.</summary>
    public AuthConfig? Auth { get; init; }

    /// <summary>Полный адрес описания API, из которого собирается каталог.</summary>
    public string DiscoveryAddress => Combine(BaseUrl,
        string.IsNullOrWhiteSpace(DiscoveryUrl) ? RestDiscovery.DefaultUrl(Discovery) : DiscoveryUrl);

    public static RestSourceSettings From(DatasourceConfig config)
    {
        var settings = config.Settings ?? [];

        return new RestSourceSettings
        {
            BaseUrl = Get(settings, DatasourceSettings.BaseUrl).TrimEnd('/'),
            Discovery = DiscoveryMode(Get(settings, DatasourceSettings.Discovery)),
            DiscoveryUrl = Get(settings, DatasourceSettings.DiscoveryUrl),
            TimeoutSec = Timeout(Get(settings, DatasourceSettings.TimeoutSec)),
            Auth = BuildAuth(config.Slug, settings),
        };
    }

    /// <summary>
    /// Складывает адрес API и путь операции, не удваивая общий префикс:
    /// <c>http://site/wp-json</c> и <c>/wp-json/wp/v2/posts</c> дают <c>http://site/wp-json/wp/v2/posts</c>.
    /// </summary>
    public static string Combine(string baseUrl, string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return baseUrl;

        var root = baseUrl.TrimEnd('/');

        // Абсолютный путь означает, что адрес источника уже подставлен в него ({{baseUrl}} в документе).
        if (Uri.TryCreate(path, UriKind.Absolute, out _))
        {
            if (root.Length == 0 || !path.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase)) return path;

            return root + "/" + TrimSharedSegment(root, path[(root.Length + 1)..]);
        }

        return root.Length == 0 ? path : root + "/" + TrimSharedSegment(root, path.TrimStart('/'));
    }

    /// <summary>Убирает из пути первый сегмент, если адрес источника заканчивается на такой же.</summary>
    static string TrimSharedSegment(string root, string relative)
    {
        var separator = relative.IndexOf('/');
        var first = separator < 0 ? relative : relative[..separator];

        if (first.Length == 0 || !root.EndsWith("/" + first, StringComparison.OrdinalIgnoreCase)) return relative;

        return separator < 0 ? "" : relative[(separator + 1)..];
    }

    /// <summary>Пустой способ сбора каталога — WordPress: это самый частый rest-источник Mars.</summary>
    static string DiscoveryMode(string value)
        => string.IsNullOrWhiteSpace(value)
            ? RestDiscovery.WordPress
            : RestDiscovery.All.FirstOrDefault(mode => string.Equals(mode, value, StringComparison.OrdinalIgnoreCase))
              ?? RestDiscovery.None;

    static int Timeout(string value)
        => int.TryParse(value, out var seconds) && seconds > 0 ? seconds : DefaultTimeoutSec;

    static AuthConfig? BuildAuth(string slug, Dictionary<string, string> settings)
    {
        var mode = Get(settings, DatasourceSettings.AuthMode);

        AuthConfig? config = mode.ToLowerInvariant() switch
        {
            RestAuthMode.Basic => new AuthConfig
            {
                Mode = AuthMode.BasicAuth,
                Username = Get(settings, DatasourceSettings.AuthUsername),
                Password = Get(settings, DatasourceSettings.AuthPassword),
            },
            RestAuthMode.Bearer => new AuthConfig
            {
                Mode = AuthMode.BearerToken,
                TokenUrl = Get(settings, DatasourceSettings.AuthTokenUrl),
                Username = Get(settings, DatasourceSettings.AuthUsername),
                Password = Get(settings, DatasourceSettings.AuthPassword),
                ClientId = NullIfEmpty(Get(settings, DatasourceSettings.AuthClientId)),
                ClientSecret = NullIfEmpty(Get(settings, DatasourceSettings.AuthClientSecret)),
                Scope = NullIfEmpty(Get(settings, DatasourceSettings.AuthScope)),
            },
            RestAuthMode.ApiKey => new AuthConfig
            {
                Mode = AuthMode.ApiKey,
                ApiKey = Get(settings, DatasourceSettings.AuthApiKey),
                ApiKeyHeaderName = DefaultIfEmpty(Get(settings, DatasourceSettings.AuthApiKeyHeader), "X-API-Key"),
            },
            RestAuthMode.CookieForm => new AuthConfig
            {
                Mode = AuthMode.CookieForm,
                LoginPageUrl = Get(settings, DatasourceSettings.AuthLoginPageUrl),
                Username = Get(settings, DatasourceSettings.AuthUsername),
                Password = Get(settings, DatasourceSettings.AuthPassword),
            },
            _ => null!,
        };

        if (config is null) return null;

        config.Id = $"datasource-{slug}";
        config.TimeoutSeconds = Timeout(Get(settings, DatasourceSettings.TimeoutSec));

        return config;
    }

    static string Get(Dictionary<string, string> settings, string key)
        => settings.TryGetValue(key, out var value) ? value?.Trim() ?? "" : "";

    static string? NullIfEmpty(string value) => string.IsNullOrEmpty(value) ? null : value;

    static string DefaultIfEmpty(string value, string fallback) => string.IsNullOrEmpty(value) ? fallback : value;
}
