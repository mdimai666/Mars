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

    /// <summary>Складывает адрес API и путь операции; абсолютный путь возвращает как есть.</summary>
    public static string Combine(string baseUrl, string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return baseUrl;
        if (Uri.TryCreate(path, UriKind.Absolute, out _)) return path;
        if (string.IsNullOrWhiteSpace(baseUrl)) return path;

        // Адресом бывает переменная документа ({{baseUrl}}) — её как URI не собрать, просто склеиваем.
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
        {
            return baseUrl.TrimEnd('/') + "/" + path.TrimStart('/');
        }

        return new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), path.TrimStart('/')).OriginalString;
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
