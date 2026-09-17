using System.Text.RegularExpressions;
using Mars.Datasource.Contracts.Models;

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
            Parameters = parameters,
            DefaultLanguage = DatasourceLanguage.Http,
            DefaultQuery = DefaultQuery(method, path),
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
