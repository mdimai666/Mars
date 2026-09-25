using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Mars.HttpSmartAuthFlow;
using Mars.HttpSmartAuthFlow.Handlers;

namespace Mars.Datasource.Providers.Rest;

/// <summary>Ответ rest-источника: статус, тело и заголовки — из них собирается результат запроса.</summary>
public class RestResponse
{
    public string Method { get; init; } = "";
    public string Url { get; init; } = "";
    public int StatusCode { get; init; }
    public string ReasonPhrase { get; init; } = "";
    public bool IsSuccess { get; init; }
    public string Body { get; init; } = "";
    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();

    public string? Header(string name)
        => Headers.TryGetValue(name, out var value) ? value : null;

    /// <summary>Общее число записей, которое сообщает WordPress.</summary>
    public long? Total => long.TryParse(Header("X-WP-Total"), out var total) ? total : null;
}

/// <summary>Выполнение HTTP-запроса rest-источника.</summary>
public static class RestExecutor
{
    public static async Task<RestResponse> SendAsync(HttpClient client, HttpRequestMessage message,
        CancellationToken cancellationToken = default)
    {
        var url = message.RequestUri?.ToString() ?? "";
        var method = message.Method.Method;

        using var response = await client.SendAsync(message, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase);

        foreach (var header in response.Headers) headers[header.Key] = string.Join(", ", header.Value);
        foreach (var header in response.Content.Headers) headers.TryAdd(header.Key, string.Join(", ", header.Value));

        return new RestResponse
        {
            Method = method,
            Url = url,
            StatusCode = (int)response.StatusCode,
            ReasonPhrase = response.ReasonPhrase ?? response.StatusCode.ToString(),
            IsSuccess = response.IsSuccessStatusCode,
            Body = body,
            Headers = headers,
        };
    }
}

/// <summary>
/// Клиенты rest-источников: стратегия доступа хранит токен и куки, поэтому клиент на источник один
/// и живёт, пока не поменялись настройки (ключ — отпечаток настроек).
/// </summary>
public class RestHttpClientCache : IDisposable
{
    /// <summary>Соединения пересоздаём, чтобы источник не «залипал» на старом DNS.</summary>
    static readonly TimeSpan ConnectionLifetime = TimeSpan.FromMinutes(5);

    readonly ConcurrentDictionary<string, HttpClient> _clients = new(StringComparer.Ordinal);

    public virtual HttpClient Get(RestSourceSettings settings) => _clients.GetOrAdd(Key(settings), _ => Create(settings));

    public void Invalidate()
    {
        foreach (var key in _clients.Keys.ToList())
        {
            if (_clients.TryRemove(key, out var client)) client.Dispose();
        }
    }

    static HttpClient Create(RestSourceSettings settings)
    {
        var inner = new SocketsHttpHandler
        {
            PooledConnectionLifetime = ConnectionLifetime,
            CookieContainer = new CookieContainer(),
            UseCookies = true,
            AllowAutoRedirect = true,
        };

        HttpMessageHandler handler = settings.Auth is { } auth
            ? new AuthFlowHandler(new AuthStrategyFactory().Create(auth)) { InnerHandler = inner }
            : inner;

        return new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(settings.TimeoutSec),
        };
    }

    static string Key(RestSourceSettings settings)
    {
        var description = string.Join('|',
            settings.BaseUrl,
            settings.Discovery,
            settings.DiscoveryUrl,
            settings.TimeoutSec,
            settings.Auth is null ? "" : JsonSerializer.Serialize(settings.Auth));

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(description)));
    }

    public void Dispose()
    {
        Invalidate();
        GC.SuppressFinalize(this);
    }
}
