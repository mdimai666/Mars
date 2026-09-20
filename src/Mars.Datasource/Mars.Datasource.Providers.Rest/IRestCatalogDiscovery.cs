using System.Text.RegularExpressions;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;

namespace Mars.Datasource.Providers.Rest;

/// <summary>
/// Сборщик каталога операций по описанию API. Один интерфейс на все способы discovery:
/// индекс WordPress, документ OpenAPI, позже — introspection GraphQL и прочее.
/// </summary>
public interface IRestCatalogDiscovery
{
    /// <summary>Значение <see cref="RestSourceSettings.Discovery"/>, которое поддерживает сборщик.</summary>
    string Mode { get; }

    /// <summary>Скачать описание API и собрать из него группы операций.</summary>
    Task<IReadOnlyList<DatasourceCatalogGroup>> DiscoverAsync(HttpClient client, string address, CancellationToken cancellationToken = default);
}

/// <summary>Операция каталога: идентификатор и заготовка запроса — общие для всех способов discovery.</summary>
public static partial class RestCatalogOperation
{
    /// <summary>Идентификатор операции: <c>METHOD /path</c>, путь с параметрами в <c>{}</c>.</summary>
    public static string Id(string method, string path) => $"{method.ToUpperInvariant()} {path}";

    public static DatasourceCatalogObject Operation(string method, string path, List<DatasourceOperationParameter> parameters)
        => new()
        {
            Id = Id(method, path),
            Name = Id(method, path),
            ObjectType = DatasourceObjectType.Operation,
            DefaultLanguage = DatasourceLanguage.Http,
            DefaultQuery = DefaultQuery(method, path),
            Operation = new DatasourceOperation
            {
                Method = method.ToUpperInvariant(),
                Parameters = parameters,
            },
        };

    /// <summary>
    /// Заготовка запроса для редактора: адрес источника переменной <c>{{baseUrl}}</c>,
    /// шаблоны пути <c>{id}</c> — переменными документа <c>{{id}}</c>.
    /// </summary>
    public static string DefaultQuery(string method, string path)
    {
        var address = PathTemplate().Replace(path.TrimStart('/'), match => "{{" + match.Groups[1].Value + "}}");

        return $"{method.ToUpperInvariant()} {BaseUrlVariable}/{address}";
    }

    /// <summary>Переменная документа с адресом источника — как она записывается в запросе.</summary>
    const string BaseUrlVariable = "{{baseUrl}}";

    [GeneratedRegex(@"\{(\w+)\}")]
    private static partial Regex PathTemplate();
}

/// <summary>
/// Общий префикс путей операций. Описание API задаёт пути не от адреса сайта, а от своего корня:
/// индекс WordPress по адресу <c>/wp-json/wp/v2</c> отдаёт маршруты вида <c>/wp/v2/posts</c>,
/// то есть к ним нужен префикс <c>/wp-json</c>, иначе запрос уходит в никуда (404).
/// </summary>
public static class RestRoutePrefix
{
    /// <summary>
    /// Префикс из адреса описания API: от адреса индекса отрезается путь его namespace
    /// (<c>/wp-json/wp/v2</c> + <c>wp/v2</c> → <c>/wp-json</c>; адрес <c>/wp-json</c> остаётся собой).
    /// </summary>
    public static string FromAddress(string? address, string? namespaceName = null)
    {
        var path = PathOf(address);

        if (path.Length == 0) return "";

        if (!string.IsNullOrWhiteSpace(namespaceName))
        {
            var suffix = "/" + namespaceName.Trim('/');

            if (path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) return path[..^suffix.Length];
        }

        return path;
    }

    /// <summary>Путь адреса без хвостового слэша; адрес бывает и относительным (server в OpenAPI).</summary>
    static string PathOf(string? address)
    {
        if (string.IsNullOrWhiteSpace(address)) return "";

        var path = (Uri.TryCreate(address, UriKind.Absolute, out var uri) ? uri.AbsolutePath : address).TrimEnd('/');

        return path == "/" ? "" : path;
    }
}
