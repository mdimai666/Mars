using System.Text.Json.Nodes;
using Mars.Datasource.Contracts.Models;
using Microsoft.OpenApi;

namespace Mars.Datasource.Providers.Rest;

/// <summary>
/// Каталог по документу OpenAPI (swagger.json / openapi.yaml): операции из <c>paths</c>,
/// параметры из <c>parameters</c> и схемы тела запроса. Подходит любому API с описанием —
/// от PostgREST (Supabase) до собственного swagger.
/// </summary>
public class OpenApiRestDiscovery : IRestCatalogDiscovery
{
    public string Mode => RestDiscovery.OpenApi;

    public async Task<IReadOnlyList<DatasourceCatalogGroup>> DiscoverAsync(HttpClient client, string address,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new InvalidOperationException("Не задан адрес документа OpenAPI");
        }

        using var response = await client.GetAsync(address, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new InvalidOperationException(
                $"Документ OpenAPI: {(int)response.StatusCode} {response.StatusCode} по адресу {address}. {Short(error)}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        // Формат (JSON или YAML) читатель определяет сам по содержимому.
        var result = await OpenApiDocument.LoadAsync(stream, cancellationToken: cancellationToken);

        if (result.Document is null)
        {
            throw new InvalidOperationException($"Не разобрать документ OpenAPI по адресу {address}: {Errors(result)}");
        }

        return Build(result.Document);
    }

    /// <summary>Группы операций из документа: группа — первый тег операции, иначе первый сегмент пути.</summary>
    public static IReadOnlyList<DatasourceCatalogGroup> Build(OpenApiDocument document)
    {
        Dictionary<string, DatasourceCatalogGroup> groups = new(StringComparer.OrdinalIgnoreCase);

        foreach (var (path, item) in document.Paths ?? new OpenApiPaths())
        {
            foreach (var (method, operation) in item.Operations)
            {
                var name = GroupName(path, operation);

                if (!groups.TryGetValue(name, out var group))
                {
                    group = new DatasourceCatalogGroup { Name = name };
                    groups[name] = group;
                }

                group.Objects.Add(RestCatalogOperation.Operation(method.Method, path, Parameters(operation)));
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

    static string GroupName(string path, OpenApiOperation operation)
    {
        var tag = operation.Tags?.FirstOrDefault(tag => !string.IsNullOrWhiteSpace(tag.Name))?.Name;

        if (!string.IsNullOrWhiteSpace(tag)) return tag;

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        return segments.Length == 0 ? "api" : segments[0];
    }

    static List<DatasourceOperationParameter> Parameters(OpenApiOperation operation)
    {
        var parameters = (operation.Parameters ?? [])
            .Select(parameter => new DatasourceOperationParameter
            {
                Name = parameter.Name ?? "",
                In = In(parameter.In),
                Type = Type(parameter.Schema),
                Required = parameter.Required,
                Default = Text(parameter.Schema?.Default),
                Enum = Enum(parameter.Schema),
                Description = parameter.Description,
            })
            .ToList();

        var body = operation.RequestBody?.Content?.Values.FirstOrDefault()?.Schema;

        if (body?.Properties is null) return parameters;

        foreach (var (name, schema) in body.Properties)
        {
            if (parameters.Any(parameter => parameter.Name == name)) continue;

            parameters.Add(new DatasourceOperationParameter
            {
                Name = name,
                In = DatasourceParameterIn.Body,
                Type = Type(schema),
                // Обязательность поля тела в OpenAPI — в списке required самой схемы
                Required = body.Required?.Contains(name) == true,
                Default = Text(schema?.Default),
                Enum = Enum(schema),
                Description = schema?.Description,
            });
        }

        return parameters;
    }

    static string In(ParameterLocation? location) => location switch
    {
        ParameterLocation.Path => DatasourceParameterIn.Path,
        ParameterLocation.Header => DatasourceParameterIn.Header,
        _ => DatasourceParameterIn.Query,
    };

    static string Type(IOpenApiSchema? schema)
        => schema?.Type?.ToString().ToLowerInvariant() is { Length: > 0 } type ? type : "string";

    static List<string>? Enum(IOpenApiSchema? schema)
        => schema?.Enum is { Count: > 0 } values
            ? values.Select(Text).Where(value => value is { Length: > 0 }).Select(value => value!).ToList()
            : null;

    static string? Text(JsonNode? node) => node?.ToString();

    static string Errors(Microsoft.OpenApi.Reader.ReadResult result)
    {
        var errors = result.Diagnostic?.Errors;

        return errors is null || errors.Count == 0 ? "документ пуст или не того формата" : errors[0].Message;
    }

    static string Short(string body) => body.Length <= 300 ? body : body[..300] + "…";
}
