using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mars.Datasource.Abstractions.Services;
using Mars.Datasource.Contracts.Ai;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;

namespace Mars.AiChat.Host.Tools;

/// <summary>
/// Инструменты агента: доступ к источникам данных через IDatasourceService —
/// основная БД Mars (slug "default"), настроенные SQL-базы, файлы и REST API.
/// </summary>
public class MarsSqlTools
{
    /// <summary>Максимум строк результата, отдаваемых модели.</summary>
    private const int MaxRows = 25;

    /// <summary>Бюджет символов на результат запроса.</summary>
    private const int MaxResultChars = 30_000;

    /// <summary>Бюджет символов на текст схемы источника.</summary>
    private const int MaxSchemaChars = 20_000;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Ключевые слова, с которых начинается читающий запрос.</summary>
    private static readonly HashSet<string> ReadKeywords =
    [
        "SELECT", "WITH", "SHOW", "TABLE", "VALUES", "EXPLAIN", "DESCRIBE", "PRAGMA",
    ];

    private readonly IDatasourceService _datasourceService;
    private readonly IDatasourceRegistry _datasourceRegistry;

    public MarsSqlTools(IDatasourceService datasourceService, IDatasourceRegistry datasourceRegistry)
    {
        _datasourceService = datasourceService;
        _datasourceRegistry = datasourceRegistry;
    }

    [Description("Список доступных источников данных: slug, название, kind (sql|file|rest), драйвер и возможности. " +
                 "Slug 'default' — основная база самого сайта Mars (посты, настройки, пользователи и т.д.). " +
                 "К sql-источникам работай через execute_sql, к file/rest — через get_source_schema и run_query.")]
    public string ListDataSources()
    {
        try
        {
            var list = _datasourceRegistry.ListSelectDatasource()
                .Select(d => new
                {
                    slug = d.Slug,
                    title = d.Title,
                    kind = d.Kind,
                    driver = d.Driver,
                    features = string.Join(",", ProfileFor(d.Kind, d.Driver)?.Features ?? []),
                });

            return JsonSerializer.Serialize(list, SerializerOptions)
                   + " Используй значение slug как параметр slug в других инструментах источников.";
        }
        catch (Exception ex)
        {
            return "Не удалось получить список источников: " + ex.GetBaseException().Message;
        }
    }

    [Description("Схема источника в компактном тексте: объекты (таблицы, файлы, операции API), их поля и параметры операций. " +
                 "Работает для любого kind'а. Вызывай до составления запроса, чтобы знать реальные имена и типы. " +
                 "На больших каталогах передавай filter — подстроку для отбора объектов.")]
    public async Task<string> GetSourceSchema(
        [Description("Slug источника из результата list_data_sources, например 'default'")] string slug,
        [Description("Фильтр объектов по подстроке (имя таблицы/операции); пустой — весь каталог")] string filter = "")
    {
        try
        {
            var catalog = await _datasourceService.Catalog(slug);

            return DatasourceSchemaText.Build(catalog, filter, MaxSchemaChars);
        }
        catch (Exception ex)
        {
            return $"Не удалось получить схему источника '{slug}': {ex.GetBaseException().Message}";
        }
    }

    [Description("Выполнить запрос к источнику file или rest по его каталогу. " +
                 "rest: objectId — операция вида 'GET /wp/v2/posts', параметры — parametersJson. " +
                 "file: objectId — файл или 'файл#Лист', query — предикат Dynamic LINQ (пустой — все строки). " +
                 "Для sql-источников используй execute_sql. Результат — строки как JSON (не более 25) и total, если источник его сообщил. " +
                 "Перед записывающим вызовом (POST/PUT/PATCH/DELETE) обязательно покажи его пользователю " +
                 "и получи подтверждение через ask_user, если он сам не разрешил выполнять без подтверждений.")]
    public async Task<string> RunQuery(
        [Description("Slug источника из результата list_data_sources")] string slug,
        [Description("Идентификатор объекта каталога: у rest — 'METHOD путь', у file — ссылка на файл или 'файл#Лист'")] string? objectId = null,
        [Description("Текст запроса: у file — предикат Dynamic LINQ, например \"Val.Num(age) > 30\"")] string? query = null,
        [Description("Параметры операции JSON-объектом имя→значение, например {\"page\":\"2\",\"per_page\":\"10\"}")] string? parametersJson = null)
    {
        try
        {
            var source = FindSource(slug);
            if (source is null)
                return $"Источник '{slug}' не найден. Вызови list_data_sources для списка доступных.";

            if (string.Equals(source.Kind, DatasourceKind.Sql, StringComparison.OrdinalIgnoreCase))
                return $"Источник '{slug}' — sql: к нему используй execute_sql со свободным SQL-запросом.";

            List<DatasourceParam>? parameters;
            try
            {
                parameters = DatasourceParametersJson.Parse(parametersJson);
            }
            catch (Exception ex)
            {
                return $"Параметры должны быть JSON-объектом имя→значение: {ex.Message}";
            }

            var request = new DatasourceRequest
            {
                ObjectId = string.IsNullOrWhiteSpace(objectId) ? null : objectId.Trim(),
                Language = ProfileFor(source.Kind, source.Driver)?.DefaultLanguage ?? DatasourceLanguage.Linq,
                Query = query?.Trim() ?? "",
                Parameters = parameters,
                MaxRows = MaxRows,
            };

            var result = await _datasourceService.Query(slug, request);
            if (!result.Ok)
                return $"Ошибка запроса к '{slug}': {result.Message}";

            return FormatResult(result);
        }
        catch (Exception ex)
        {
            return $"Не удалось выполнить запрос к источнику '{slug}': {ex.GetBaseException().Message}";
        }
    }

    [Description("Выполнить один SQL-запрос к sql-источнику. Slug 'default' — основная база самого сайта Mars. " +
                 "Для SELECT возвращает строки как JSON (не более 25 строк — добавляй LIMIT; total — сколько всего насчитал источник), " +
                 "для INSERT/UPDATE/DELETE/DDL — число затронутых строк. Схему таблиц бери через get_source_schema. " +
                 "Перед записывающим запросом (INSERT/UPDATE/DELETE/DROP/TRUNCATE/ALTER) обязательно покажи точный SQL пользователю " +
                 "и получи подтверждение через ask_user, если он сам не разрешил выполнять без подтверждений.")]
    public async Task<string> ExecuteSql(
        [Description("Slug базы из результата list_data_sources, например 'default'")] string slug,
        [Description("Один SQL-запрос")] string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return "Пустой SQL-запрос.";

        try
        {
            if (IsReadQuery(sql))
            {
                var result = await _datasourceService.Query(slug, new DatasourceRequest { Query = sql, MaxRows = MaxRows });
                if (!result.Ok)
                    return $"Ошибка SQL: {result.Message}";

                return FormatResult(result);
            }

            var nonQuery = await _datasourceService.Modify(slug, new DatasourceRequest
            {
                Language = DatasourceLanguage.Sql,
                Query = sql,
            });
            if (!nonQuery.Ok)
                return $"Ошибка SQL: {nonQuery.Message}";

            return $"OK. Затронуто строк: {nonQuery.RowsAffected}.";
        }
        catch (Exception ex)
        {
            return $"Не удалось выполнить SQL к базе '{slug}': {ex.GetBaseException().Message}";
        }
    }

    SelectDatasourceDto? FindSource(string slug)
        => _datasourceRegistry.ListSelectDatasource()
            .FirstOrDefault(d => string.Equals(d.Slug, slug, StringComparison.OrdinalIgnoreCase));

    /// <summary>Профиль провайдера источника: точная пара (kind, driver), иначе первый профиль kind'а.</summary>
    DatasourceKindProfile? ProfileFor(string kind, string driver)
    {
        var profiles = _datasourceRegistry.Providers();

        return profiles.FirstOrDefault(p =>
                   string.Equals(p.Kind, kind, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(p.Driver, driver, StringComparison.OrdinalIgnoreCase))
               ?? profiles.FirstOrDefault(p => string.Equals(p.Kind, kind, StringComparison.OrdinalIgnoreCase));
    }

    static bool IsReadQuery(string sql)
    {
        var firstWord = new string(sql.TrimStart()
            .TakeWhile(char.IsLetter)
            .ToArray())
            .ToUpperInvariant();

        return ReadKeywords.Contains(firstWord);
    }

    /// <summary>
    /// Результат запроса → компактный JSON для модели: строки-объекты с ограничением по числу строк
    /// и размеру, total (если источник сообщил), ответ документом у источников без таблицы.
    /// </summary>
    static string FormatResult(QueryResultDto result)
    {
        if (result.Rows.Length == 0 && !string.IsNullOrEmpty(result.Json))
            return WithTotal("Ответ документом: " + Truncate(result.Json, MaxResultChars), result.Total);

        var headers = result.Fields.Select(field => field.Name).ToArray();

        var rows = new List<Dictionary<string, string?>>();
        var budget = MaxResultChars;
        var sizeTruncated = false;

        foreach (var row in result.Rows.Take(MaxRows))
        {
            var dict = new Dictionary<string, string?>();
            for (var i = 0; i < headers.Length && i < row.Length; i++)
                dict[headers[i]] = row[i];

            var rowJson = JsonSerializer.Serialize(dict, SerializerOptions);
            if (rows.Count > 0 && budget - rowJson.Length < 0)
            {
                sizeTruncated = true;
                break;
            }

            budget -= rowJson.Length;
            rows.Add(dict);
        }

        var payload = JsonSerializer.Serialize(new { rowCount = result.Rows.Length, total = result.Total, rows }, SerializerOptions);

        var notes = new List<string>();
        if (result.Rows.Length > rows.Count)
            notes.Add(sizeTruncated
                ? $"Показаны {rows.Count} строк из {result.Rows.Length} (результат сокращён по размеру) — уточни запрос: WHERE, конкретные колонки, LIMIT."
                : $"Показаны первые {rows.Count} строк из {result.Rows.Length} — добавь LIMIT или уточни запрос.");
        if (result.Truncated)
            notes.Add("Источник обрезал результат своим лимитом строк.");

        return notes.Count == 0 ? payload : payload + "\n" + string.Join(" ", notes);
    }

    static string WithTotal(string text, long? total)
        => total is null ? text : $"{text}\nВсего записей у источника: {total}.";

    static string Truncate(string text, int maxChars)
        => text.Length <= maxChars ? text : text[..maxChars] + $"… (обрезано, всего {text.Length} символов)";
}
