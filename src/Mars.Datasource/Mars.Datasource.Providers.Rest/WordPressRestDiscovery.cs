using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Mars.Datasource.Contracts.Models;

namespace Mars.Datasource.Providers.Rest;

/// <summary>
/// Каталог REST API WordPress: индекс <c>/wp-json/wp/v2</c> отдаёт <c>routes → endpoints → args</c>.
/// OpenAPI-документа у WordPress нет, поэтому описание разбираем как JSON.
/// </summary>
public partial class WordPressRestDiscovery : IRestCatalogDiscovery
{
    public string Mode => RestDiscovery.WordPress;

    public async Task<IReadOnlyList<DatasourceCatalogGroup>> DiscoverAsync(HttpClient client, string address,
        CancellationToken cancellationToken = default)
        => Build(await RestDiscoveryHttp.GetStringAsync(client, address, "индекс REST API WordPress", cancellationToken));

    /// <summary>Группы операций из индекса: группа — пространство имён (<c>wp/v2</c>), объект — <c>METHOD path</c>.</summary>
    public static IReadOnlyList<DatasourceCatalogGroup> Build(string json)
    {
        var routes = JsonNode.Parse(json)?["routes"] as JsonObject
            ?? throw new InvalidOperationException("В ответе нет \"routes\": это не индекс REST API WordPress");

        Dictionary<string, DatasourceCatalogGroup> groups = new(StringComparer.OrdinalIgnoreCase);

        foreach (var (routePath, routeNode) in routes)
        {
            var route = routeNode as JsonObject;

            var namespaceName = Text(route?["namespace"]);
            var groupName = string.IsNullOrWhiteSpace(namespaceName) ? FallbackGroup(routePath) : namespaceName;

            if (!groups.TryGetValue(groupName, out var group))
            {
                group = new DatasourceCatalogGroup { Name = groupName };
                groups[groupName] = group;
            }

            var (path, pathParameters) = NormalizePath(routePath);

            foreach (var endpointNode in (route?["endpoints"] as JsonArray) ?? new JsonArray())
            {
                if (endpointNode is not JsonObject endpoint) continue;

                var args = endpoint["args"] as JsonObject;

                foreach (var method in Methods(endpoint))
                {
                    var read = IsRead(method);

                    group.Objects.Add(RestCatalogOperation.Operation(
                        method,
                        path,
                        Parameters(args, pathParameters, read)));
                }
            }
        }

        return groups.Values
            .OrderBy(group => group.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => new DatasourceCatalogGroup
            {
                Name = group.Name,
                Objects = group.Objects.OrderBy(obj => obj.Id, StringComparer.OrdinalIgnoreCase).ToList(),
            })
            .ToList();
    }

    /// <summary>
    /// Путь маршрута WordPress — регулярное выражение: <c>/wp/v2/posts/(?P&lt;id&gt;[\d]+)</c>.
    /// Приводим его к виду OpenAPI (<c>/wp/v2/posts/{id}</c>) и запоминаем параметры пути.
    /// </summary>
    public static (string Path, List<string> Parameters) NormalizePath(string routePath)
    {
        List<string> parameters = [];

        var path = RouteParameter().Replace(routePath, match =>
        {
            var name = match.Groups[1].Value;
            parameters.Add(name);

            return "{" + name + "}";
        });

        return (path, parameters);
    }

    static List<string> Methods(JsonObject endpoint)
        => ((endpoint["methods"] as JsonArray) ?? new JsonArray())
            .Select(node => Text(node))
            .Where(method => method is { Length: > 0 })
            .Select(method => method!.ToUpperInvariant())
            .Distinct()
            .ToList();

    static bool IsRead(string method) => method is "GET" or "HEAD" or "OPTIONS";

    static List<DatasourceOperationParameter> Parameters(JsonObject? args, List<string> pathParameters, bool read)
    {
        var parameters = pathParameters
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(name => new DatasourceOperationParameter
            {
                Name = name,
                In = DatasourceParameterIn.Path,
                Type = "string",
                Required = true,
                Description = "параметр пути",
            })
            .ToList();

        if (args is null) return parameters;

        foreach (var (name, node) in args)
        {
            if (pathParameters.Contains(name, StringComparer.OrdinalIgnoreCase)) continue;
            if (parameters.Any(parameter => parameter.Name == name)) continue;

            var arg = node as JsonObject;

            parameters.Add(new DatasourceOperationParameter
            {
                Name = name,
                // GET читает аргументы из строки запроса, остальное — поля тела
                In = read ? DatasourceParameterIn.Query : DatasourceParameterIn.Body,
                Type = Text(arg?["type"]) ?? "string",
                Required = Boolean(arg?["required"]),
                Default = Text(arg?["default"]),
                Enum = (arg?["enum"] as JsonArray)?
                    .Select(item => Text(item))
                    .Where(value => value is { Length: > 0 })
                    .Select(value => value!)
                    .ToList(),
                Description = Text(arg?["description"]),
            });
        }

        return parameters;
    }

    static string FallbackGroup(string routePath)
    {
        var segments = routePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        return segments.Length == 0 ? "api" : segments[0];
    }

    static bool Boolean(JsonNode? node)
        => node is JsonValue value && value.TryGetValue<bool>(out var flag) && flag;

    /// <summary>Значение как текст: строку без кавычек, объект и массив — их JSON.</summary>
    static string? Text(JsonNode? node) => node switch
    {
        null => null,
        JsonValue value when value.TryGetValue<string>(out var text) => text,
        JsonValue value => value.ToString(),
        _ => node.ToJsonString(),
    };

    [GeneratedRegex(@"\(\?P<(\w+)>[^)]*\)")]
    private static partial Regex RouteParameter();
}

/// <summary>Скачивание описания API с внятной ошибкой: адрес и статус нужны в сообщении.</summary>
static class RestDiscoveryHttp
{
    public static async Task<string> GetStringAsync(HttpClient client, string address, string what,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new InvalidOperationException($"Не задан адрес, откуда брать {what}");
        }

        using var response = await client.GetAsync(address, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"{what}: {(int)response.StatusCode} {response.StatusCode} по адресу {address}. {Short(body)}");
        }

        return body;
    }

    static string Short(string body) => body.Length <= 300 ? body : body[..300] + "…";
}
