using System.Diagnostics;
using System.Text.Json;
using Mars.Datasource.Abstractions.Interfaces;
using Mars.Datasource.Abstractions.Models;
using Mars.Datasource.Contracts.Models;

namespace Mars.Datasource.Providers.Rest;

/// <summary>
/// REST API как источник данных. Операции каталога приходят из описания API (индекс WordPress
/// или документ OpenAPI) и из документа запросов пользователя <c>requests.http</c>. Текст запроса —
/// HTTP-запрос в синтаксисе VS Code REST Client, параметры запроса — переменные <c>{{name}}</c>.
/// </summary>
public class RestDatasourceProvider : IDatasourceDiscoverableProvider
{
    /// <summary>Группа каталога с запросами пользователя из документа.</summary>
    public const string DocumentGroupName = "Запросы";

    static readonly string[] KnownMethods = ["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS"];

    readonly DatasourceConfig _config;
    readonly RestSourceSettings _settings;
    readonly IDatasourceStore _store;
    readonly RestHttpClientCache _clients;
    readonly IReadOnlyList<IRestCatalogDiscovery> _discoveries;

    public RestDatasourceProvider(DatasourceConfig config, IDatasourceStore store, RestHttpClientCache clients,
        IEnumerable<IRestCatalogDiscovery> discoveries)
    {
        _config = config;
        _settings = RestSourceSettings.From(config);
        _store = store;
        _clients = clients;
        _discoveries = discoveries.ToList();
    }

    public DatasourceCapabilities Capabilities { get; } = new()
    {
        CanQuery = true,
        CanBrowse = true,
        CanWrite = true,
    };

    /// <summary>Каталог: сохранённый discovery (или свежий, если его ещё нет) плюс документ запросов.</summary>
    public async Task<DatasourceCatalog> Catalog(CancellationToken cancellationToken = default)
    {
        var groups = await SavedGroupsAsync(cancellationToken) ?? await FetchAndSaveGroupsAsync(cancellationToken);

        return Build(await WithDocumentAsync(groups, cancellationToken));
    }

    /// <summary>Перечитать описание API заново и сохранить его — «обновить» в дереве объектов.</summary>
    public async Task<DatasourceCatalog> Discover(CancellationToken cancellationToken = default)
        => Build(await WithDocumentAsync(await FetchAndSaveGroupsAsync(cancellationToken), cancellationToken));

    public async Task<QueryResultDto> Query(DatasourceRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var document = ParseDocument(request.Query);
            var http = Resolve(document, request);

            using var message = RestRequestBuilder.Build(document, http, _settings, request.Parameters);
            var response = await SendAsync(message, request, cancellationToken);

            return RestResponseMapping.Map(response, $"{http.Method} {http.Url}", stopwatch.ElapsedMilliseconds, request.MaxRows);
        }
        catch (Exception ex)
        {
            return new QueryResultDto
            {
                Ok = false,
                Message = QueryResultMapping.Error(ex),
                DatabaseDriver = DatasourceKind.Rest,
                Command = request.Query,
                ElapsedMs = stopwatch.ElapsedMilliseconds,
            };
        }
    }

    public async Task<SqlNonQueryResultActionDto> Modify(DatasourceRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var document = ParseDocument(request.Query);
            var http = Resolve(document, request);

            using var message = RestRequestBuilder.Build(document, http, _settings, request.Parameters);
            var response = await SendAsync(message, request, cancellationToken);

            return new SqlNonQueryResultActionDto
            {
                Ok = response.IsSuccess,
                Message = $"HTTP {response.StatusCode} {response.ReasonPhrase} · {http.Method} {http.Url}",
                DatabaseDriver = DatasourceKind.Rest,
            };
        }
        catch (Exception ex)
        {
            return new SqlNonQueryResultActionDto
            {
                Ok = false,
                Message = QueryResultMapping.Error(ex),
                DatabaseDriver = DatasourceKind.Rest,
            };
        }
    }

    //=== каталог ==============================================================

    async Task<IReadOnlyList<DatasourceCatalogGroup>> FetchGroupsAsync(CancellationToken cancellationToken)
    {
        if (string.Equals(_settings.Discovery, RestDiscovery.None, StringComparison.OrdinalIgnoreCase)) return [];

        var discovery = _discoveries.FirstOrDefault(item => string.Equals(item.Mode, _settings.Discovery, StringComparison.OrdinalIgnoreCase))
            ?? throw new NotSupportedException($"Способ сбора каталога \"{_settings.Discovery}\" не подключён");

        // Discovery ходит в сеть, поэтому адрес обязан быть абсолютным: без адреса API идти некуда.
        if (!Uri.IsWellFormedUriString(_settings.DiscoveryAddress, UriKind.Absolute))
        {
            throw new InvalidOperationException($"Не задан адрес API источника \"{_config.Slug}\"");
        }

        return await discovery.DiscoverAsync(_clients.Get(_settings), _settings.DiscoveryAddress, cancellationToken);
    }

    /// <summary>Описание API стоит похода в сеть, поэтому собранный каталог сохраняем в data-корень.</summary>
    async Task<IReadOnlyList<DatasourceCatalogGroup>> FetchAndSaveGroupsAsync(CancellationToken cancellationToken)
    {
        var groups = await FetchGroupsAsync(cancellationToken);

        await SaveGroupsAsync(groups, cancellationToken);

        return groups;
    }

    async Task<IReadOnlyList<DatasourceCatalogGroup>?> SavedGroupsAsync(CancellationToken cancellationToken)
    {
        var saved = await _store.ReadTextAsync(_config.Slug, DatasourceSettings.CatalogDocument, cancellationToken);

        if (string.IsNullOrWhiteSpace(saved)) return null;

        try
        {
            return JsonSerializer.Deserialize<List<DatasourceCatalogGroup>>(saved);
        }
        catch (JsonException)
        {
            // Битый каталог — не причина не пускать в источник: соберём заново.
            return null;
        }
    }

    Task SaveGroupsAsync(IReadOnlyList<DatasourceCatalogGroup> groups, CancellationToken cancellationToken)
        => _store.WriteTextAsync(_config.Slug, DatasourceSettings.CatalogDocument,
            JsonSerializer.Serialize(groups, RestJson.Indented), cancellationToken);

    /// <summary>
    /// Запросы пользователя из документа — отдельная группа. Ошибка разбора показывается в дереве,
    /// а не блокирует источник: документ правят руками.
    /// </summary>
    async Task<List<DatasourceCatalogGroup>> WithDocumentAsync(IReadOnlyList<DatasourceCatalogGroup> groups,
        CancellationToken cancellationToken)
    {
        List<DatasourceCatalogGroup> result = [.. groups];

        DatasourceCatalogGroup group = new() { Name = DocumentGroupName };

        try
        {
            var document = await DocumentAsync(cancellationToken);

            foreach (var request in document.Requests)
            {
                var id = Unique(result, request.Name ?? request.Title);

                group.Objects.Add(new DatasourceCatalogObject
                {
                    Id = id,
                    Name = id,
                    ObjectType = DatasourceObjectType.Operation,
                    DefaultLanguage = DatasourceLanguage.Http,
                    DefaultQuery = request.Raw,
                    // Границы блока: дерево переходит к запросу в редакторе, не переписывая документ
                    Line = request.Line,
                    EndLine = request.EndLine,
                });
            }
        }
        catch (HttpDocumentException ex)
        {
            group.Objects.Add(new DatasourceCatalogObject
            {
                Id = DatasourceSettings.RequestsDocument,
                Name = $"{DatasourceSettings.RequestsDocument}: {ex.Message}",
                ObjectType = DatasourceObjectType.Operation,
                DefaultLanguage = DatasourceLanguage.Http,
            });
        }

        if (group.Objects.Count > 0) result.Add(group);

        return result;
    }

    async Task<HttpDocument> DocumentAsync(CancellationToken cancellationToken)
    {
        var text = await _store.ReadTextAsync(_config.Slug, DatasourceSettings.RequestsDocument, cancellationToken);

        return ParseDocument(text);
    }

    DatasourceCatalog Build(List<DatasourceCatalogGroup> groups) => new()
    {
        Kind = string.IsNullOrWhiteSpace(_config.Kind) ? DatasourceKind.Rest : _config.Kind,
        SourceName = _config.Label,
        Capabilities = Capabilities,
        Groups = groups,
    };

    /// <summary>Идентификатор объекта должен быть один на каталог: дерево ищет объект по нему.</summary>
    static string Unique(IEnumerable<DatasourceCatalogGroup> groups, string id)
    {
        var taken = groups.SelectMany(group => group.Objects).Select(obj => obj.Id).ToHashSet(StringComparer.Ordinal);

        if (!taken.Contains(id)) return id;

        for (var index = 2; ; index++)
        {
            var candidate = $"{id} ({index})";

            if (!taken.Contains(candidate)) return candidate;
        }
    }

    //=== выполнение ===========================================================

    static HttpDocument ParseDocument(string? text)
        => string.IsNullOrWhiteSpace(text) ? new HttpDocument() : HttpDocumentParser.Parse(text);

    /// <summary>
    /// Что выполнять: запрос из текста редактора (по имени, если открыт именованный запрос документа,
    /// иначе первый в документе) или операция каталога по идентификатору, когда текст пуст.
    /// </summary>
    HttpDocumentRequest Resolve(HttpDocument document, DatasourceRequest request)
    {
        if (document.Requests.Count > 0)
        {
            return Find(document, request.ObjectId) ?? document.Requests[0];
        }

        if (!string.IsNullOrWhiteSpace(request.ObjectId)) return FromObjectId(request.ObjectId);

        throw new InvalidOperationException("Пустой запрос: напишите HTTP-запрос в редакторе или выберите операцию в дереве");
    }

    static HttpDocumentRequest? Find(HttpDocument document, string? objectId)
    {
        if (string.IsNullOrWhiteSpace(objectId)) return null;

        return document.Requests.FirstOrDefault(item =>
            string.Equals(item.Name, objectId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.Title, objectId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Идентификатор операции — <c>METHOD path</c>; путь разрешается относительно адреса API.</summary>
    static HttpDocumentRequest FromObjectId(string objectId)
    {
        var parts = objectId.Split(' ', 2, StringSplitOptions.TrimEntries);

        return parts.Length == 2 && KnownMethods.Contains(parts[0].ToUpperInvariant())
            ? new HttpDocumentRequest { Method = parts[0].ToUpperInvariant(), Url = parts[1], Name = objectId }
            : new HttpDocumentRequest { Method = "GET", Url = objectId, Name = objectId };
    }

    async Task<RestResponse> SendAsync(HttpRequestMessage message, DatasourceRequest request, CancellationToken cancellationToken)
    {
        var client = _clients.Get(_settings);

        if (request.TimeoutSec is not > 0) return await RestExecutor.SendAsync(client, message, cancellationToken);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(request.TimeoutSec.Value));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        return await RestExecutor.SendAsync(client, message, linked.Token);
    }
}
