using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Mars.Datasource.Contracts.Models;

namespace Mars.Datasource.Providers.Rest;

/// <summary>
/// HTTP-запрос из документа и параметров: переменные <c>{{name}}</c> и шаблоны пути <c>{name}</c>
/// раскрываются значениями параметров, а не упомянутые в тексте параметры уходят в строку запроса
/// (чтение) или в JSON-тело (запись). Так форма параметров работает и с заготовкой из каталога,
/// и с запросом, который человек написал руками.
/// </summary>
public static partial class RestRequestBuilder
{
    /// <summary>Переменная с адресом источника: <c>{{baseUrl}}</c> в документе запросов.</summary>
    public const string BaseUrlVariable = "baseUrl";

    public static HttpRequestMessage Build(HttpDocument document, HttpDocumentRequest request,
        RestSourceSettings settings, IReadOnlyList<DatasourceParam>? parameters)
    {
        var values = Values(parameters);

        if (!values.ContainsKey(BaseUrlVariable) && !string.IsNullOrWhiteSpace(settings.BaseUrl))
        {
            values[BaseUrlVariable] = settings.BaseUrl;
        }

        var expanded = HttpDocumentParser.Expand(document, request, values);
        var mentioned = Mentioned(request);

        var url = ApplyPathTemplates(expanded.Url, values);
        url = RestSourceSettings.Combine(settings.BaseUrl, url);

        var spare = values
            .Where(pair => !mentioned.Contains(pair.Key))
            .Where(pair => !string.Equals(pair.Key, BaseUrlVariable, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var body = expanded.Body;

        if (body is null && expanded.IsWrite && spare.Count > 0)
        {
            body = JsonObject(spare);
            spare = [];
        }
        else if (!expanded.IsWrite && spare.Count > 0)
        {
            url = AddQuery(url, spare);
            spare = [];
        }

        var message = new HttpRequestMessage(new HttpMethod(expanded.Method), url);

        if (body is not null)
        {
            // Content-Type принадлежит телу: задаём его сразу, иначе StringContent оставит text/plain.
            var contentType = expanded.Headers
                .FirstOrDefault(header => header.Name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                ?.Value;

            message.Content = new StringContent(body, Encoding.UTF8,
                string.IsNullOrWhiteSpace(contentType) ? "application/json" : contentType);
        }

        foreach (var header in expanded.Headers)
        {
            if (message.Content is not null && header.Name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)) continue;

            if (message.Headers.TryAddWithoutValidation(header.Name, header.Value)) continue;

            message.Content?.Headers.TryAddWithoutValidation(header.Name, header.Value);
        }

        return message;
    }

    static Dictionary<string, string?> Values(IReadOnlyList<DatasourceParam>? parameters)
    {
        Dictionary<string, string?> values = new(StringComparer.OrdinalIgnoreCase);

        foreach (var parameter in parameters ?? [])
        {
            if (string.IsNullOrWhiteSpace(parameter.Name)) continue;

            values[parameter.Name.Trim()] = parameter.Value;
        }

        return values;
    }

    /// <summary>Имена, которые документ уже использует: подстановка в текст важнее формы параметров.</summary>
    static HashSet<string> Mentioned(HttpDocumentRequest request)
    {
        var text = string.Join('\n',
        [
            request.Url,
            .. request.Headers.Select(header => header.Name + ": " + header.Value),
            request.Body ?? "",
        ]);

        return VariableName()
            .Matches(text)
            .Select(match => match.Groups[1].Success ? match.Groups[1].Value.Trim() : match.Groups[2].Value.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    static string ApplyPathTemplates(string url, Dictionary<string, string?> values)
        => PathTemplate().Replace(url, match =>
        {
            var name = match.Groups[1].Value;

            return values.TryGetValue(name, out var value) && value is not null
                ? Uri.EscapeDataString(value)
                : match.Value;
        });

    static string AddQuery(string url, IReadOnlyCollection<KeyValuePair<string, string?>> values)
    {
        var query = string.Join("&", values
            .Where(pair => !HasQueryKey(url, pair.Key))
            .Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value ?? "")}"));

        if (query.Length == 0) return url;

        return url + (url.Contains('?') ? "&" : "?") + query;
    }

    static bool HasQueryKey(string url, string name)
    {
        var start = url.IndexOf('?');
        if (start < 0) return false;

        return url[(start + 1)..]
            .Split('&')
            .Select(part => Uri.UnescapeDataString(part.Split('=', 2)[0]))
            .Any(key => string.Equals(key, name, StringComparison.OrdinalIgnoreCase));
    }

    static string JsonObject(IReadOnlyCollection<KeyValuePair<string, string?>> values)
    {
        JsonObject body = new();

        foreach (var pair in values) body[pair.Key] = Scalar(pair.Value);

        return body.ToJsonString(RestJson.Indented);
    }

    /// <summary>Значение параметра в тело: число и булево — своим типом, остальное строкой.</summary>
    static JsonNode? Scalar(string? value)
    {
        if (value is null) return null;
        if (bool.TryParse(value, out var flag)) return JsonValue.Create(flag);
        if (long.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var integer)) return JsonValue.Create(integer);
        if (double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var number)) return JsonValue.Create(number);

        return JsonValue.Create(value);
    }

    [GeneratedRegex(@"\{\{\s*([^{}]+?)\s*\}\}|\{(\w+)\}")]
    private static partial Regex VariableName();

    [GeneratedRegex(@"\{(\w+)\}")]
    private static partial Regex PathTemplate();
}
