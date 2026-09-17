using System.Text.RegularExpressions;

namespace Mars.Datasource.Providers.Rest;

/// <summary>
/// Разбор документа <c>.http</c> (синтаксис VS Code REST Client): блоки через <c>###</c>,
/// имя запроса <c># @name</c>, переменные <c>@name = value</c> и подстановки <c>{{name}}</c>.
/// </summary>
public static partial class HttpDocumentParser
{
    const string Separator = "###";
    const string NameDirective = "@name";

    /// <summary>Предел раскрытия вложенных переменных: защищает от цикла <c>@a = {{b}}</c>, <c>@b = {{a}}</c>.</summary>
    const int MaxExpandDepth = 10;

    public static HttpDocument Parse(string? text)
    {
        HttpDocument document = new();

        if (string.IsNullOrWhiteSpace(text)) return document;

        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        Block? block = null;

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            var trimmed = line.TrimStart();
            var number = index + 1;

            if (trimmed.StartsWith(Separator, StringComparison.Ordinal))
            {
                Add(document, block);

                block = new Block
                {
                    Label = trimmed[Separator.Length..].Trim(),
                    Line = number,
                };

                continue;
            }

            // Строка запроса уже была: дальше заголовки и тело, их не разбираем
            if (block?.RequestLine is not null)
            {
                block.Lines.Add(line);
                continue;
            }

            if (IsVariable(trimmed, out var name, out var value))
            {
                if (block is null) document.Variables[name] = value;
                else block.Variables[name] = value;

                continue;
            }

            if (IsDirective(trimmed, NameDirective, out var requestName))
            {
                if (block is not null) block.Name = requestName;

                continue;
            }

            if (IsComment(trimmed)) continue;

            if (trimmed.Length == 0) continue;

            // У первого запроса разделителя может не быть
            block ??= new Block { Line = number };

            block.RequestLine = number;
            block.Request = trimmed;
            block.RawRequest = line;
        }

        Add(document, block);

        return document;
    }

    /// <summary>
    /// Раскрывает переменные запроса: значения документа, затем значения блока, затем переданные
    /// извне (параметры запроса перекрывают документ). Неизвестная переменная — ошибка.
    /// </summary>
    public static HttpDocumentRequest Expand(HttpDocument document, HttpDocumentRequest request,
        IReadOnlyDictionary<string, string?>? values = null)
    {
        Dictionary<string, string> variables = new(StringComparer.OrdinalIgnoreCase);

        foreach (var (name, value) in document.Variables) variables[name] = value;
        foreach (var (name, value) in request.Variables) variables[name] = value;

        foreach (var (name, value) in values ?? new Dictionary<string, string?>())
        {
            if (value is not null) variables[name] = value;
        }

        return new HttpDocumentRequest
        {
            Name = request.Name,
            Method = request.Method,
            Url = Expand(request.Url, variables, 0),
            Headers = request.Headers
                .Select(header => new HttpDocumentHeader(Expand(header.Name, variables, 0), Expand(header.Value, variables, 0)))
                .ToList(),
            Body = request.Body is null ? null : Expand(request.Body, variables, 0),
            Variables = request.Variables,
            Line = request.Line,
            Raw = request.Raw,
        };
    }

    static string Expand(string text, Dictionary<string, string> variables, int depth)
    {
        if (depth >= MaxExpandDepth) throw new HttpDocumentException($"Слишком глубокая вложенность переменных (>{MaxExpandDepth})");
        if (!text.Contains("{{", StringComparison.Ordinal)) return text;

        return VariableRegex().Replace(text, match =>
        {
            var name = match.Groups[1].Value.Trim();

            if (name.StartsWith('$')) return SystemVariable(name);

            if (!variables.TryGetValue(name, out var value))
            {
                throw new HttpDocumentException($"Не задана переменная \"{name}\"");
            }

            return Expand(value, variables, depth + 1);
        });
    }

    /// <summary>Встроенные переменные REST Client. Переменных окружения здесь нет намеренно:
    /// документ приходит из браузера и не должен читать секреты сервера.</summary>
    static string SystemVariable(string name) => name switch
    {
        "$guid" => Guid.NewGuid().ToString(),
        "$timestamp" => DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
        "$isoTimestamp" => DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
        "$datetime" => DateTimeOffset.UtcNow.ToString("R"),
        "$randomInt" => Random.Shared.Next(0, 1000).ToString(),
        _ => throw new HttpDocumentException($"Неизвестная системная переменная \"{name}\""),
    };

    static void Add(HttpDocument document, Block? block)
    {
        if (block is null) return;

        if (block.Request is null)
        {
            if (block.Lines.Count > 0 && !string.IsNullOrWhiteSpace(string.Concat(block.Lines)))
            {
                throw new HttpDocumentException($"Строка {block.Line}: в блоке нет строки запроса (METHOD address)");
            }

            return;
        }

        var (method, url) = SplitRequestLine(block.Request, block.RequestLine ?? 0);

        var bodyStart = 0;
        var headers = new List<HttpDocumentHeader>();

        for (; bodyStart < block.Lines.Count; bodyStart++)
        {
            var line = block.Lines[bodyStart].Trim();

            if (line.Length == 0)
            {
                bodyStart++;
                break;
            }

            if (!HeaderRegex().IsMatch(line)) break;

            var separator = line.IndexOf(':');
            headers.Add(new HttpDocumentHeader(line[..separator].Trim(), line[(separator + 1)..].Trim()));
        }

        var body = string.Join('\n', block.Lines.Skip(bodyStart)).TrimEnd();

        document.Requests.Add(new HttpDocumentRequest
        {
            Name = string.IsNullOrWhiteSpace(block.Name) ? NullIfEmpty(block.Label) : block.Name,
            Method = method,
            Url = url,
            Headers = headers,
            Body = string.IsNullOrWhiteSpace(body) ? null : body,
            Variables = block.Variables,
            Line = block.Line,
            Raw = string.Join('\n', new[] { block.RawRequest }.Concat(block.Lines)).TrimEnd(),
        });
    }

    static (string Method, string Url) SplitRequestLine(string line, int number)
    {
        var match = RequestLineRegex().Match(line);

        if (match.Success)
        {
            return (match.Groups["method"].Value.ToUpperInvariant(), match.Groups["url"].Value);
        }

        if (UrlOnlyRegex().IsMatch(line)) return ("GET", line);

        throw new HttpDocumentException($"Строка {number}: не разобрать строку запроса \"{line}\"");
    }

    static bool IsComment(string trimmed)
        => trimmed.StartsWith('#') || trimmed.StartsWith("//", StringComparison.Ordinal);

    static bool IsDirective(string trimmed, string directive, out string value)
    {
        value = "";

        if (!trimmed.StartsWith('#')) return false;

        var rest = trimmed.TrimStart('#', ' ', '\t');

        if (!rest.StartsWith(directive, StringComparison.OrdinalIgnoreCase)) return false;

        value = rest[directive.Length..].Trim();

        return true;
    }

    static bool IsVariable(string trimmed, out string name, out string value)
    {
        name = "";
        value = "";

        var match = VariableDeclarationRegex().Match(trimmed);

        if (!match.Success) return false;

        name = match.Groups[1].Value;
        value = match.Groups[2].Value.Trim();

        return true;
    }

    static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    [GeneratedRegex(@"\{\{\s*([^{}]+?)\s*\}\}")]
    private static partial Regex VariableRegex();

    [GeneratedRegex(@"^(?<method>[A-Za-z]+)\s+(?<url>\S+)\s*(?:HTTP/\d(?:\.\d)?)?$")]
    private static partial Regex RequestLineRegex();

    [GeneratedRegex(@"^(https?://|/|\{\{)")]
    private static partial Regex UrlOnlyRegex();

    [GeneratedRegex(@"^@([A-Za-z_][\w\-.]*)\s*=\s*(.*)$")]
    private static partial Regex VariableDeclarationRegex();

    [GeneratedRegex(@"^[!#$%&'*+\-.^_`|~0-9A-Za-z]+\s*:")]
    private static partial Regex HeaderRegex();

    class Block
    {
        public string Label { get; init; } = "";
        public int Line { get; init; }
        public string? Name { get; set; }
        public string? Request { get; set; }
        public string RawRequest { get; set; } = "";
        public int? RequestLine { get; set; }
        public List<string> Lines { get; } = [];
        public Dictionary<string, string> Variables { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>Ошибка разбора документа запросов: номер строки и что не так.</summary>
public class HttpDocumentException(string message) : Exception(message);
