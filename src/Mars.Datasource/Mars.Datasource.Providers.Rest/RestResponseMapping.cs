using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Mars.Datasource.Abstractions.Models;
using Mars.Datasource.Contracts.Models;

namespace Mars.Datasource.Providers.Rest;

/// <summary>
/// Ответ rest-источника в общий результат: массив объектов раскладывается в колонки и строки,
/// документ (объект, текст, ошибка) отдаётся в <see cref="QueryResultDto.Json"/>.
/// </summary>
public static class RestResponseMapping
{
    /// <summary>Предел тела, которое отдаём документом: мегабайты JSON в редакторе не нужны.</summary>
    public const int MaxJsonChars = 1_000_000;

    /// <summary>Сколько значений смотрим, чтобы вывести тип колонки.</summary>
    public const int SampleValues = 200;

    public static QueryResultDto Map(RestResponse response, string command, long elapsedMs, int maxRows)
    {
        QueryResultDto result = new()
        {
            DatabaseDriver = DatasourceKind.Rest,
            Command = command,
            ElapsedMs = elapsedMs,
            Total = response.Total,
        };

        if (!response.IsSuccess)
        {
            result.Ok = false;
            result.Message = $"HTTP {response.StatusCode} {response.ReasonPhrase} · {response.Method} {response.Url} · {Short(response.Body)}";
            result.Json = Document(response.Body);

            return result;
        }

        var body = TryParse(response.Body);

        if (body is JsonArray array)
        {
            FillArray(result, array, maxRows);
        }
        else
        {
            FillDocument(result, body, response.Body);
        }

        result.Ok = true;
        result.Message = string.IsNullOrWhiteSpace(response.Body)
            ? $"HTTP {response.StatusCode}: пустой ответ"
            : $"HTTP {response.StatusCode} {response.ReasonPhrase}";

        return result;
    }

    /// <summary>Массив объектов — таблица; массив значений — одна колонка <c>value</c>.</summary>
    static void FillArray(QueryResultDto result, JsonArray array, int maxRows)
    {
        var items = array.Where(node => node is JsonObject).Cast<JsonObject>().ToList();

        if (items.Count > 0 && items.Count == array.Count)
        {
            var take = Take(items.Count, maxRows);
            var shown = items.Take(take).ToList();

            result.Columns = Columns(shown);
            result.Rows = shown
                .Select(item => result.Columns.Select(column => Text(item[column.Name])).ToArray())
                .ToArray();
            result.Truncated = items.Count > take;

            return;
        }

        var values = array.Select(Text).ToArray();
        var limit = Take(values.Length, maxRows);

        result.Columns = [Column("value", array.Take(limit).ToList())];
        result.Rows = values.Take(limit).Select(value => new[] { value }).ToArray();
        result.Truncated = values.Length > limit;
    }

    /// <summary>Ответ не массивом: объект показываем одной строкой, а полное тело оставляем в JSON-виде.</summary>
    static void FillDocument(QueryResultDto result, JsonNode? body, string raw)
    {
        result.Json = Document(raw);

        if (body is JsonObject item)
        {
            result.Columns = Columns([item]);
            result.Rows = [result.Columns.Select(column => Text(item[column.Name])).ToArray()];

            return;
        }

        if (body is not null)
        {
            result.Columns = [Column("value", [body])];
            result.Rows = [[Text(body)]];

            return;
        }

        // Не JSON: показываем текст ответа одной ячейкой.
        var text = raw.Length > MaxJsonChars ? raw[..MaxJsonChars] + "…" : raw;

        result.Columns = [new QueryColumn { Name = "response", DataTypeName = JsonTypeInference.Text, ClrTypeName = typeof(string).FullName!, IsNullable = true }];
        result.Rows = string.IsNullOrWhiteSpace(text) ? [] : [[text]];
        result.Truncated = raw.Length > MaxJsonChars;
    }

    static QueryColumn[] Columns(IReadOnlyList<JsonObject> items)
    {
        List<string> names = [];
        HashSet<string> seen = new(StringComparer.Ordinal);

        foreach (var item in items)
        {
            foreach (var (name, _) in item)
            {
                if (seen.Add(name)) names.Add(name);
            }
        }

        return names
            .Select(name => Column(name, items.Take(SampleValues).Select(item => item[name]).ToList()))
            .ToArray();
    }

    static QueryColumn Column(string name, IReadOnlyList<JsonNode?> values)
    {
        var typeName = JsonTypeInference.Infer(values);

        return new QueryColumn
        {
            Name = name,
            DataTypeName = typeName,
            ClrTypeName = QColumnMapping.ClrType(typeName).FullName ?? typeof(string).FullName!,
            IsNullable = true,
            IsJson = QColumnMapping.IsJson(typeName),
        };
    }

    /// <summary>Значение ячейки инвариантным текстом: объект и массив — их JSON.</summary>
    public static string? Text(JsonNode? node) => node switch
    {
        null => null,
        JsonValue value when value.TryGetValue<string>(out var text) => text,
        JsonValue value when value.TryGetValue<bool>(out var flag) => flag ? "true" : "false",
        JsonValue value when value.TryGetValue<long>(out var integer) => integer.ToString(CultureInfo.InvariantCulture),
        JsonValue value when value.TryGetValue<double>(out var number) => number.ToString(CultureInfo.InvariantCulture),
        JsonValue value => value.ToString(),
        _ => node.ToJsonString(RestJson.Options),
    };

    static string Document(string body)
        => body.Length > MaxJsonChars ? body[..MaxJsonChars] + "…" : body;

    static string Short(string body)
    {
        var text = body.Trim();

        return text.Length <= 300 ? text : text[..300] + "…";
    }

    static JsonNode? TryParse(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;

        try
        {
            return JsonNode.Parse(body, documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        }
        catch (JsonException)
        {
            return null;
        }
    }

    static int Take(int count, int maxRows) => maxRows > 0 ? Math.Min(count, maxRows) : count;
}

/// <summary>
/// Тип колонки по JSON-значениям. Имена — из числа понятных <c>QColumnMapping</c>, поэтому
/// короткое имя типа и подсветка значений в гриде работают без отдельных правил для rest.
/// </summary>
public static class JsonTypeInference
{
    public const string Text = "text";
    public const string BigInt = "bigint";
    public const string Double = "double precision";
    public const string Boolean = "boolean";
    public const string Timestamp = "timestamp";
    public const string Json = "jsonb";

    public static string Infer(IEnumerable<JsonNode?> values)
    {
        var samples = values
            .Where(value => value is not null)
            .Take(RestResponseMapping.SampleValues)
            .ToList();

        if (samples.Count == 0) return Text;
        if (samples.Any(value => value is JsonObject or JsonArray)) return Json;
        if (samples.All(value => value is JsonValue { } number && number.TryGetValue<long>(out _))) return BigInt;
        if (samples.All(value => value is JsonValue { } number && number.TryGetValue<double>(out _))) return Double;
        if (samples.All(value => value is JsonValue { } flag && flag.TryGetValue<bool>(out _))) return Boolean;
        if (samples.All(IsDate)) return Timestamp;

        return Text;
    }

    /// <summary>Строка датой: REST API отдают даты текстом (<c>2024-03-01T10:20:30</c>).</summary>
    static bool IsDate(JsonNode? node)
        => node is JsonValue value
           && value.TryGetValue<string>(out var text)
           && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out _);
}
