using System.Net;
using System.Text.Json;
using Mars.Core.Exceptions;
using Mars.Plugin.Contracts.Catalog;
using Mars.Plugin.Options;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mars.Plugin.Handlers;

internal sealed class PluginCatalogClient : IPluginCatalogClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<PluginCatalogOption> _options;
    private readonly ILogger<PluginCatalogClient> _logger;

    public PluginCatalogClient(
        IHttpClientFactory httpClientFactory,
        IOptions<PluginCatalogOption> options,
        ILogger<PluginCatalogClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public bool IsEnabled => _options.Value.Enabled && !string.IsNullOrWhiteSpace(_options.Value.Url);

    public Task<CatalogPagedResponse<CatalogPluginDto>?> SearchAsync(
        MarketplaceSearchRequest query, string? marsVersion, CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string?>();
        if (!string.IsNullOrWhiteSpace(query.Q)) parameters["q"] = query.Q;
        if (!string.IsNullOrWhiteSpace(query.Tag)) parameters["tag"] = query.Tag;
        if (query.Recommended == true) parameters["recommended"] = "true";
        if (!string.IsNullOrWhiteSpace(query.Sort)) parameters["sort"] = query.Sort;
        if (!string.IsNullOrWhiteSpace(marsVersion)) parameters["minVersion"] = marsVersion;
        if (query.Page is not null) parameters["page"] = query.Page.ToString();
        if (query.Take is not null) parameters["take"] = query.Take.ToString();

        return GetAsync<CatalogPagedResponse<CatalogPluginDto>>("api/plugins", parameters, cancellationToken);
    }

    public Task<CatalogPluginDto?> GetAsync(string packageId, CancellationToken cancellationToken)
        => GetAsync<CatalogPluginDto>($"api/plugins/{Uri.EscapeDataString(packageId)}", new Dictionary<string, string?>(), cancellationToken);

    public Task<CatalogPagedResponse<CatalogReviewDto>?> GetReviewsAsync(
        string packageId, int? page, int? take, CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string?>();
        if (page is not null) parameters["page"] = page.ToString();
        if (take is not null) parameters["take"] = take.ToString();

        return GetAsync<CatalogPagedResponse<CatalogReviewDto>>(
            $"api/plugins/{Uri.EscapeDataString(packageId)}/reviews", parameters, cancellationToken);
    }

    private async Task<T?> GetAsync<T>(string path, IDictionary<string, string?> parameters, CancellationToken cancellationToken)
        where T : class
    {
        if (!IsEnabled) return null;

        var baseUrl = _options.Value.Url.TrimEnd('/');
        var url = QueryHelpers.AddQueryString($"{baseUrl}/{path}", parameters);

        try
        {
            using var http = _httpClientFactory.CreateClient();
            http.Timeout = RequestTimeout;

            using var response = await http.GetAsync(url, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Plugin catalog '{Url}' returned {Status} for '{Path}'", baseUrl, (int)response.StatusCode, path);
                throw new UserActionException($"Каталог плагинов ответил ошибкой {(int)response.StatusCode}.");
            }

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Plugin catalog '{Url}' is unreachable ('{Path}')", baseUrl, path);
            throw new UserActionException($"Каталог плагинов недоступен: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Plugin catalog '{Url}' timed out ('{Path}')", baseUrl, path);
            throw new UserActionException("Каталог плагинов не ответил вовремя.");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Plugin catalog '{Url}' returned an invalid response ('{Path}')", baseUrl, path);
            throw new UserActionException("Каталог плагинов вернул некорректный ответ.");
        }
    }
}
