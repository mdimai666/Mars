using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Mars.Datasource.Host.Services;

namespace Mars.AiChat.Host.Tools;

/// <summary>
/// Инструменты агента: доступ к SQL-базам через IDatasourceService —
/// основная БД Mars (slug "default") и настроенные data sources.
/// </summary>
public class MarsSqlTools
{
    /// <summary>Максимум строк результата, отдаваемых модели.</summary>
    private const int MaxRows = 50;

    /// <summary>Бюджет символов на результат запроса и на схему.</summary>
    private const int MaxResultChars = 30_000;

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };

    /// <summary>Ключевые слова, с которых начинается читающий запрос.</summary>
    private static readonly HashSet<string> ReadKeywords =
    [
        "SELECT", "WITH", "SHOW", "TABLE", "VALUES", "EXPLAIN", "DESCRIBE", "PRAGMA",
    ];

    private readonly IDatasourceService _datasourceService;

    public MarsSqlTools(IDatasourceService datasourceService)
    {
        _datasourceService = datasourceService;
    }

    [Description("Список доступных SQL-баз: slug, название и драйвер. " +
                 "Slug 'default' — основная база самого сайта Mars (посты, настройки, пользователи и т.д.). " +
                 "Вызывай перед работой с SQL, чтобы узнать slug нужной базы.")]
    public string ListDataSources()
    {
        try
        {
            var list = _datasourceService.ListSelectDatasource()
                .Select(d => new { slug = d.Slug, title = d.Title, driver = d.Driver });

            return JsonSerializer.Serialize(list, SerializerOptions)
                   + " Используй значение slug как параметр slug в других SQL-инструментах.";
        }
        catch (Exception ex)
        {
            return "Не удалось получить список баз: " + ex.GetBaseException().Message;
        }
    }

    [Description("Структура базы данных: таблицы и их колонки. " +
                 "Для больших баз передай tablesFilter — подстроку для фильтрации имён таблиц.")]
    public async Task<string> GetDatabaseSchema(
        [Description("Slug базы из результата list_data_sources, например 'default'")] string slug,
        [Description("Фильтр таблиц: подстрока в имени таблицы/схемы. Пустая строка — все таблицы.")] string tablesFilter = "")
    {
        try
        {
            var structure = await _datasourceService.DatabaseStructure(slug);

            var tables = structure.Tables
                .Where(t => string.IsNullOrWhiteSpace(tablesFilter)
                            || t.TableName.Contains(tablesFilter, StringComparison.OrdinalIgnoreCase)
                            || (t.TableSchema?.SchemaName.Contains(tablesFilter, StringComparison.OrdinalIgnoreCase) ?? false))
                .OrderBy(t => t.TableSchema?.SchemaName ?? "")
                .ThenBy(t => t.TableName);

            var sb = new StringBuilder();
            sb.Append("База: ").Append(structure.DatabaseName).AppendLine(". Таблицы и колонки:");

            var shown = 0;
            foreach (var table in tables)
            {
                var name = string.IsNullOrEmpty(table.TableSchema?.SchemaName)
                    ? table.TableName
                    : $"{table.TableSchema.SchemaName}.{table.TableName}";

                var columns = table.Columns is null
                    ? ""
                    : string.Join(", ", table.Columns.Values.OrderBy(c => c.ColumnOrdinal).Select(c => c.ColumnName));

                var line = $"{name}: {columns}";
                if (sb.Length + line.Length > MaxResultChars)
                {
                    sb.AppendLine("…");
                    sb.AppendLine("Схема сокращена: уточни фильтр tablesFilter (например, имя нужной таблицы), чтобы увидеть остальные таблицы.");
                    break;
                }

                sb.AppendLine(line);
                shown++;
            }

            if (shown == 0)
                return $"В базе '{slug}' не найдено таблиц по фильтру '{tablesFilter}'.";

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Не удалось получить структуру базы '{slug}': {ex.GetBaseException().Message}";
        }
    }

    [Description("Выполнить один SQL-запрос к базе. Для SELECT возвращает строки как JSON (не более 50 строк — добавляй LIMIT), " +
                 "для INSERT/UPDATE/DELETE/DDL — число затронутых строк. " +
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
            var isRead = IsReadQuery(sql);

            if (isRead)
            {
                var result = await _datasourceService.SqlQuery(slug, sql);
                if (!result.Ok)
                    return $"Ошибка SQL: {result.Message}";

                return FormatRows(result.Data);
            }

            var nonQuery = await _datasourceService.SqlNonQuery(slug, sql);
            if (!nonQuery.Ok)
                return $"Ошибка SQL: {nonQuery.Message}";

            return $"OK. Затронуто строк: {nonQuery.RowsAffected}.";
        }
        catch (Exception ex)
        {
            return $"Не удалось выполнить SQL к базе '{slug}': {ex.GetBaseException().Message}";
        }
    }

    static bool IsReadQuery(string sql)
    {
        var firstWord = new string(sql.TrimStart()
            .TakeWhile(c => char.IsLetter(c))
            .ToArray())
            .ToUpperInvariant();

        return ReadKeywords.Contains(firstWord);
    }

    /// <summary>
    /// string[][] (первая строка — заголовки) → JSON-массив объектов,
    /// с ограничением по числу строк и по размеру.
    /// </summary>
    static string FormatRows(string[][]? data)
    {
        if (data is null || data.Length <= 1)
            return "OK. Запрос выполнен, данных не вернул.";

        var headers = data[0];
        var totalRows = data.Length - 1;

        var rows = new List<Dictionary<string, string?>>();
        var budget = MaxResultChars;
        var sizeTruncated = false;

        foreach (var row in data.Skip(1).Take(MaxRows))
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

        var result = JsonSerializer.Serialize(new { rowCount = totalRows, rows }, SerializerOptions);

        var notes = new List<string>();
        if (totalRows > rows.Count)
            notes.Add(sizeTruncated
                ? $"Показаны {rows.Count} строк из {totalRows} (результат сокращён по размеру) — уточни запрос: WHERE, конкретные колонки, LIMIT."
                : $"Показаны первые {rows.Count} строк из {totalRows} — добавь LIMIT или уточни запрос.");

        return notes.Count == 0 ? result : result + "\n" + string.Join(" ", notes);
    }
}
