using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Mars.AiChat.Host.Tools;

/// <summary>
/// Инструмент агента: исходящие HTTP-запросы к внешним API и сервисам.
/// Запросы уходят с сервера без аутентификации пользователя.
/// </summary>
public class MarsHttpTools
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };
    private const int MaxResponseChars = 30_000;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MarsHttpTools> _logger;

    public MarsHttpTools(IHttpClientFactory httpClientFactory, ILogger<MarsHttpTools> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [Description("Выполнить HTTP-запрос и вернуть статус и тело ответа (обрезается до 30 КБ). " +
                 "Используй для внешних API и интеграций; запрос уходит с сервера без аутентификации пользователя.")]
    public async Task<string> HttpRequest(
        [Description("Метод: GET, POST, PUT, PATCH, DELETE")] string method,
        [Description("Абсолютный URL")] string url,
        [Description("Тело запроса (например JSON), опционально")] string? body = null,
        [Description("Заголовки парами 'Name=Value' через ';', опционально")] string? headers = null,
        [Description("Content-Type тела, по умолчанию application/json")] string contentType = "application/json",
        CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(new HttpMethod(method.ToUpperInvariant()), url);

            if (body is not null)
                request.Content = new StringContent(body, Encoding.UTF8, contentType);

            if (!string.IsNullOrWhiteSpace(headers))
            {
                foreach (var part in headers.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var idx = part.IndexOf('=');
                    if (idx <= 0) continue;
                    var (name, value) = (part[..idx], part[(idx + 1)..]);
                    if (!request.Headers.TryAddWithoutValidation(name, value))
                        request.Content?.Headers.TryAddWithoutValidation(name, value);
                }
            }

            _logger.LogDebug("AiChat: HTTP {Method} {Url}", request.Method, url);

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            var text = await response.Content.ReadAsStringAsync(ct);

            var truncated = text.Length > MaxResponseChars;
            return JsonSerializer.Serialize(new
            {
                status = (int)response.StatusCode,
                ok = response.IsSuccessStatusCode,
                contentType = response.Content.Headers.ContentType?.ToString() ?? "",
                bodyLength = text.Length,
                truncated,
                body = truncated ? text[..MaxResponseChars] + "…(обрезано)" : text,
            }, SerializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AiChat: HTTP {Method} {Url} failed", method, url);
            return JsonSerializer.Serialize(new { ok = false, error = ex.GetBaseException().Message }, SerializerOptions);
        }
    }
}
