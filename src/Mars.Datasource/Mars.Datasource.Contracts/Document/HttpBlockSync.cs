using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Mars.Datasource.Contracts.Document;

/// <summary>
/// Связь блока документа <c>.http</c> с операцией каталога: найти операцию по тексту блока,
/// прочитать значения её параметров из текста (query, path, header, body) и записать их обратно.
/// Чистые функции — синхронизация формы параметров с редактором живёт на фронте, правила одни.
/// Значения не экранируются: переменные документа <c>{{var}}</c> живут в тексте как есть.
/// </summary>
public static partial class HttpBlockSync
{
    /// <summary>Строка запроса блока: метод и адрес; null — в блоке нет запроса.</summary>
    public static (string Method, string Url)? RequestOf(string? blockText)
        => Parse(blockText) is { } parts ? (parts.Method, UrlOf(parts)) : null;

    /// <summary>
    /// Операция каталога, которой соответствует запрос блока. Рассматриваются только операции
    /// с параметрами (discovery); пользовательский запрос «от балды» не совпадёт ни с чем —
    /// формы у блока не будет. Из нескольких кандидатов побеждает самая конкретная
    /// (больше литеральных сегментов пути, затем длиннее путь).
    /// </summary>
    public static DatasourceCatalogObject? MatchOperation(string? blockText, IEnumerable<DatasourceCatalogObject> objects)
    {
        if (Parse(blockText) is not { } parts) return null;

        var urlSegments = PathSegments(PathOf(UrlOf(parts)));

        DatasourceCatalogObject? best = null;
        var bestLiterals = -1;
        var bestLength = -1;

        foreach (var candidate in objects)
        {
            if (candidate.Operation is not { Method.Length: > 0, Parameters.Count: > 0 }) continue;
            if (!candidate.Operation.Method.Equals(parts.Method, StringComparison.OrdinalIgnoreCase)) continue;

            var operationPath = OperationPath(candidate);

            if (operationPath.Length == 0) continue;

            var literals = PathMatch(operationPath, urlSegments);

            if (literals < 0) continue;

            var length = Segments(operationPath).Length;

            if (literals > bestLiterals || (literals == bestLiterals && length > bestLength))
            {
                bestLiterals = literals;
                bestLength = length;
                best = candidate;
            }
        }

        return best;
    }

    /// <summary>Значения параметров операции, вычитанные из текста блока; отсутствующий параметр не возвращается.</summary>
    public static Dictionary<string, string?> ReadValues(string? blockText, DatasourceCatalogObject operation)
    {
        Dictionary<string, string?> values = new(StringComparer.OrdinalIgnoreCase);

        if (Parse(blockText) is not { } parts || operation.Operation is null) return values;

        var url = UrlOf(parts);
        var pairs = QueryPairs(QueryOf(url));
        var urlSegments = PathSegments(PathOf(url));
        var operationSegments = Segments(OperationPath(operation));
        var offset = urlSegments.Length - operationSegments.Length;
        var body = BodyObject(parts);
        var headers = Headers(parts);

        foreach (var parameter in operation.Operation.Parameters)
        {
            var value = parameter.In switch
            {
                DatasourceParameterIn.Query => Find(pairs, parameter.Name),
                DatasourceParameterIn.Path => PathValue(operationSegments, urlSegments, offset, parameter.Name),
                DatasourceParameterIn.Header => headers.TryGetValue(parameter.Name, out var header) ? header : null,
                DatasourceParameterIn.Body => BodyValue(body, parameter.Name),
                _ => null,
            };

            if (value is not null) values[parameter.Name] = value;
        }

        return values;
    }

    /// <summary>
    /// Текст блока с подставленными значениями параметров: query-параметр добавляется или заменяется
    /// в адресе, шаблон пути <c>{id}</c> (и <c>{{id}}</c> заготовки) раскрывается значением,
    /// поле JSON-тела устанавливается, пустое значение параметр убирает. Не объявленные операцией
    /// части запроса не трогаются; тело не-JSON не трогается.
    /// </summary>
    public static string ApplyValues(string? blockText, DatasourceCatalogObject operation,
        IReadOnlyDictionary<string, string?> values)
    {
        if (Parse(blockText) is not { } parts || operation.Operation is null) return blockText ?? "";

        var parameters = operation.Operation.Parameters;
        var url = UrlOf(parts);
        var path = PathOf(url);
        var pairs = QueryPairs(QueryOf(url));

        foreach (var parameter in parameters.Where(item => item.In == DatasourceParameterIn.Path))
        {
            var value = Value(values, parameter.Name);

            if (string.IsNullOrWhiteSpace(value)) continue;

            // Сначала длинная форма заготовки ({{id}}), иначе замена {id} оставит от неё «{7}»
            path = path.Replace("{{" + parameter.Name + "}}", value)
                .Replace("{" + parameter.Name + "}", value);
        }

        foreach (var parameter in parameters.Where(item => item.In == DatasourceParameterIn.Query))
        {
            var value = Value(values, parameter.Name);

            if (string.IsNullOrWhiteSpace(value))
            {
                pairs.RemoveAll(pair => pair.Key.Equals(parameter.Name, StringComparison.OrdinalIgnoreCase));
                continue;
            }

            if (!TryReplace(pairs, parameter.Name, value)) pairs.Add((parameter.Name, value));
        }

        var rebuiltUrl = path + (pairs.Count > 0 ? "?" + string.Join("&", pairs.Select(pair => $"{pair.Key}={pair.Value}")) : "");

        var headers = HeaderLines(parts);

        foreach (var parameter in parameters.Where(item => item.In == DatasourceParameterIn.Header))
        {
            var value = Value(values, parameter.Name);
            var index = headers.FindIndex(line => HeaderName(line).Equals(parameter.Name, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(value))
            {
                if (index >= 0) headers.RemoveAt(index);
                continue;
            }

            if (index >= 0) headers[index] = $"{parameter.Name}: {value}";
            else headers.Add($"{parameter.Name}: {value}");
        }

        var body = BodyLines(parts);

        if (parameters.Any(item => item.In == DatasourceParameterIn.Body))
        {
            var bodyText = string.Join('\n', body).Trim();
            var json = JsonObject(bodyText);

            if (json is null && bodyText.Length == 0) json = new JsonObject();

            if (json is not null)
            {
                var changed = false;

                foreach (var parameter in parameters.Where(item => item.In == DatasourceParameterIn.Body))
                {
                    var value = Value(values, parameter.Name);

                    if (string.IsNullOrWhiteSpace(value))
                    {
                        changed |= json.Remove(parameter.Name);
                        continue;
                    }

                    json[parameter.Name] = Scalar(value);
                    changed = true;
                }

                if (changed && json.Count > 0)
                {
                    body = json.ToJsonString(IndentedJson).Replace("\r\n", "\n").Split('\n').ToList();
                }
                else if (changed)
                {
                    body = [];
                }
            }
        }

        return Build(parts, WithUrl(parts.RequestLine, rebuiltUrl), headers, body);
    }

    //=== разбор блока ==========================================================

    /// <summary>Раскладка блока: строки до запроса, строка запроса, заголовки и тело — как их видит серверный парсер.</summary>
    sealed class BlockParts
    {
        public List<string> Head = [];
        public string RequestLine = "";
        public string Method = "";
        public string Url = "";
        public List<string> Headers = [];
        public List<string> Body = [];
    }

    static BlockParts? Parse(string? blockText)
    {
        var lines = Lines(blockText).ToList();

        for (var index = 0; index < lines.Count; index++)
        {
            var trimmed = lines[index].Trim();

            if (trimmed.Length == 0
                || trimmed.StartsWith('#')
                || trimmed.StartsWith("//", StringComparison.Ordinal)
                || VariableRegex().IsMatch(trimmed)) continue;

            var match = RequestLineRegex().Match(trimmed);
            var urlOnly = !match.Success && UrlOnlyRegex().IsMatch(trimmed);

            if (!match.Success && !urlOnly) return null;

            BlockParts parts = new()
            {
                Head = [.. lines.Take(index)],
                RequestLine = lines[index],
                Method = match.Success ? match.Groups["method"].Value.ToUpperInvariant() : "GET",
                Url = match.Success ? match.Groups["url"].Value : trimmed,
            };

            var start = index + 1;

            for (; start < lines.Count; start++)
            {
                var line = lines[start].Trim();

                if (line.Length == 0)
                {
                    start++;
                    break;
                }

                if (!HeaderRegex().IsMatch(line)) break;

                parts.Headers.Add(lines[start]);
            }

            parts.Body = [.. lines.Skip(start)];

            return parts;
        }

        return null;
    }

    static string UrlOf(BlockParts parts) => parts.Url;

    static string Build(BlockParts parts, string requestLine, List<string> headers, List<string> body)
    {
        List<string> lines = [.. parts.Head, requestLine, .. headers];

        if (body.Any(line => line.Trim().Length > 0))
        {
            // Пустая строка отделяет тело от строки запроса и заголовков — требует синтаксис REST Client
            lines.Add("");
            lines.AddRange(body);
        }

        return string.Join('\n', lines);
    }

    /// <summary>Строка запроса с заменённым адресом; отступ и HTTP-версия сохраняются.</summary>
    static string WithUrl(string requestLine, string url)
    {
        var indent = requestLine[..(requestLine.Length - requestLine.TrimStart().Length)];
        var trimmed = requestLine.Trim();

        var match = RequestLineRegex().Match(trimmed);

        return match.Success
            ? indent + match.Groups["method"].Value + " " + url + match.Groups["suffix"].Value
            : indent + url;
    }

    //=== адрес: путь и query ====================================================

    static string PathOf(string url)
    {
        var index = url.IndexOf('?');

        return index < 0 ? url : url[..index];
    }

    static string QueryOf(string url)
    {
        var index = url.IndexOf('?');

        return index < 0 ? "" : url[(index + 1)..];
    }

    static List<(string Key, string Value)> QueryPairs(string query)
        => query.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Select(split => (split[0], split.Length > 1 ? split[1] : ""))
            .ToList();

    static string? Find(List<(string Key, string Value)> pairs, string name)
    {
        foreach (var pair in pairs)
        {
            if (pair.Key.Equals(name, StringComparison.OrdinalIgnoreCase)) return pair.Value;
        }

        return null;
    }

    static bool TryReplace(List<(string Key, string Value)> pairs, string name, string value)
    {
        for (var index = 0; index < pairs.Count; index++)
        {
            if (!pairs[index].Key.Equals(name, StringComparison.OrdinalIgnoreCase)) continue;

            pairs[index] = (pairs[index].Key, value);
            return true;
        }

        return false;
    }

    //=== путь операции ==========================================================

    /// <summary>Путь операции из идентификатора каталога (<c>GET /wp/v2/posts/{id}</c>).</summary>
    static string OperationPath(DatasourceCatalogObject operation)
    {
        var id = operation.Id;
        var index = id.IndexOf(' ');

        return index < 0 ? "" : id[(index + 1)..];
    }

    static string[] Segments(string path)
        => path.Split('/', StringSplitOptions.RemoveEmptyEntries);

    static string[] PathSegments(string path)
    {
        var segments = Segments(path);

        // Абсолютный адрес: «https:» и пустой сегмент после «//» не участвуют в сравнении хвоста
        return segments.Where(segment => segment.Length > 0 && !segment.EndsWith(':')).ToArray();
    }

    /// <summary>Совпадение хвоста адреса с путём операции: сколько литеральных сегментов сошлось; -1 — не совпал.</summary>
    static int PathMatch(string operationPath, string[] urlSegments)
    {
        var operationSegments = Segments(operationPath);

        if (operationSegments.Length == 0 || operationSegments.Length > urlSegments.Length) return -1;

        var offset = urlSegments.Length - operationSegments.Length;
        var literals = 0;

        for (var index = 0; index < operationSegments.Length; index++)
        {
            var segment = operationSegments[index];

            if (IsTemplate(segment)) continue;

            if (!segment.Equals(urlSegments[offset + index], StringComparison.OrdinalIgnoreCase)) return -1;

            literals++;
        }

        return literals;
    }

    static bool IsTemplate(string segment) => segment.StartsWith('{') && segment.EndsWith('}') && segment.Length > 2;

    static string? PathValue(string[] operationSegments, string[] urlSegments, int offset, string name)
    {
        if (offset < 0) return null;

        for (var index = 0; index < operationSegments.Length; index++)
        {
            if (!IsTemplate(operationSegments[index])) continue;
            if (TemplateName(operationSegments[index]) != name) continue;

            var position = offset + index;

            return position < urlSegments.Length ? urlSegments[position] : null;
        }

        return null;
    }

    static string TemplateName(string segment) => segment.Trim('{', '}');

    //=== заголовки и тело =======================================================

    static List<string> HeaderLines(BlockParts parts) => [.. parts.Headers];

    static Dictionary<string, string> Headers(BlockParts parts)
    {
        Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase);

        foreach (var line in parts.Headers)
        {
            var separator = line.IndexOf(':');

            if (separator < 0) continue;

            headers[line[..separator].Trim()] = line[(separator + 1)..].Trim();
        }

        return headers;
    }

    static string HeaderName(string line)
    {
        var separator = line.IndexOf(':');

        return separator < 0 ? line.Trim() : line[..separator].Trim();
    }

    static List<string> BodyLines(BlockParts parts) => [.. parts.Body];

    static JsonObject? BodyObject(BlockParts parts) => JsonObject(string.Join('\n', parts.Body).Trim());

    static JsonObject? JsonObject(string text)
    {
        if (text.Length == 0) return null;

        try
        {
            return JsonNode.Parse(text) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    static string? BodyValue(JsonObject? body, string name)
    {
        if (body is null || !body.TryGetPropertyValue(name, out var node) || node is null) return null;

        try
        {
            return node.GetValue<System.Text.Json.JsonElement>().ToString();
        }
        catch (InvalidOperationException)
        {
            return node.ToJsonString();
        }
    }

    /// <summary>Значение параметра в тело: число и булево — своим типом, остальное строкой (как RestRequestBuilder).</summary>
    static JsonNode? Scalar(string value)
    {
        if (bool.TryParse(value, out var flag)) return JsonValue.Create(flag);
        if (long.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var integer)) return JsonValue.Create(integer);
        if (double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var number)) return JsonValue.Create(number);

        return JsonValue.Create(value);
    }

    static readonly JsonSerializerOptions IndentedJson = new() { WriteIndented = true };

    static string? Value(IReadOnlyDictionary<string, string?> values, string name)
        => values.TryGetValue(name, out var value) ? value?.Trim() : null;

    static string[] Lines(string? text) => (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

    // Регулярки — как в серверном HttpDocumentParser: блок обязан читаться одинаково с обеих сторон.

    [GeneratedRegex(@"^(?<method>[A-Za-z]+)\s+(?<url>\S+)(?<suffix>\s+HTTP/\d(?:\.\d)?)?$")]
    private static partial Regex RequestLineRegex();

    [GeneratedRegex(@"^(https?://|/|\{\{)")]
    private static partial Regex UrlOnlyRegex();

    [GeneratedRegex(@"^@[A-Za-z_][\w\-.]*\s*=")]
    private static partial Regex VariableRegex();

    [GeneratedRegex(@"^[!#$%&'*+\-.^_`|~0-9A-Za-z]+\s*:")]
    private static partial Regex HeaderRegex();
}
