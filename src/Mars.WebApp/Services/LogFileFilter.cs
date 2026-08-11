using System.Globalization;
using System.Text.RegularExpressions;

namespace Mars.Services;

/// <summary>
/// Разбор и фильтрация записей лога приложения (формат NReco.Logging.File:
/// "2026-08-11T00:00:19.0445964+09:00	WARN	[Category]	[EventId]	сообщение").
/// Понимает и старый формат "2026-08-11 14:30:22.123 [7] WARN Logger[0] - сообщение".
/// Многострочные записи (например stack trace) не разрываются.
/// </summary>
public static class LogFileFilter
{
    public const string LogFilePattern = "app_*.log";
    public const string LogFileNamePrefix = "app_";

    public const string PeriodHour = "1h";
    public const string Period6Hours = "6h";
    public const string PeriodDay = "1d";
    public const string PeriodWeek = "7d";
    public const string PeriodMonth = "30d";

    /// <summary>Канонические уровни логов.</summary>
    public static readonly string[] Levels = ["TRACE", "DEBUG", "INFO", "WARN", "ERROR", "CRITICAL"];

    static readonly Regex EntryStartRegex = new(
        @"^(?<ts>\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}:\d{2}(?:[.,]\d+)?(?:Z|[+-]\d{1,2}:?\d{2})?)",
        RegexOptions.Compiled);

    static readonly char[] TokenSeparators = ['\t', ' '];

    /// <summary>
    /// Код периода ("1h", "6h", "1d", "7d", "30d") в TimeSpan.
    /// Пустое или неизвестое значение — null (без фильтра).
    /// </summary>
    public static TimeSpan? ParsePeriod(string? period) => period switch
    {
        PeriodHour => TimeSpan.FromHours(1),
        Period6Hours => TimeSpan.FromHours(6),
        PeriodDay => TimeSpan.FromDays(1),
        PeriodWeek => TimeSpan.FromDays(7),
        PeriodMonth => TimeSpan.FromDays(30),
        _ => null,
    };

    /// <summary>
    /// Csv уровней ("warn,error") в множество канонических уровней.
    /// Пустое значение — null (без фильтра).
    /// </summary>
    public static HashSet<string>? ParseLevels(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return null;

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var level = TryNormalizeLevel(part);
            if (level is not null) set.Add(level);
        }

        return set.Count > 0 ? set : null;
    }

    /// <summary>
    /// Читает строки лога и возвращает только записи указанных уровней и не старше <paramref name="since"/>.
    /// Значение null означает отсутствие соответствующего фильтра.
    /// </summary>
    public static IEnumerable<string> FilterLines(TextReader reader, IReadOnlyCollection<string>? levels, DateTime? since)
    {
        var entry = new List<string>();
        var timestamp = default(DateTime);
        var level = "";

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var match = EntryStartRegex.Match(line);
            if (match.Success)
            {
                foreach (var entryLine in EntryLinesIfPass(entry, timestamp, level, levels, since))
                    yield return entryLine;

                entry.Clear();
                timestamp = ParseTimestamp(match.Groups["ts"].Value);
                level = ExtractLevel(line);
            }

            entry.Add(line);
        }

        foreach (var entryLine in EntryLinesIfPass(entry, timestamp, level, levels, since))
            yield return entryLine;
    }

    /// <summary>
    /// Бесшовное чтение дневных файлов app_*.log: от новейших к старейшим, записи фильтруются
    /// по уровням и времени, суммарно не более <paramref name="maxLines"/> строк,
    /// результат в хронологическом порядке.
    /// </summary>
    public static string[] ReadSeamless(string logsDir, IReadOnlyCollection<string>? levels, DateTime? since, int maxLines)
    {
        if (string.IsNullOrEmpty(logsDir) || !Directory.Exists(logsDir)) return [];

        var files = Directory.EnumerateFiles(logsDir, LogFilePattern)
            .OrderByDescending(f => Path.GetFileName(f), StringComparer.Ordinal);

        var chunks = new List<string[]>();
        var totalLines = 0;

        foreach (var file in files)
        {
            // файл app_YYYY-MM-DD.log содержит записи только этого дня
            if (since is not null && TryGetFileDate(file, out var fileDate) && fileDate < since.Value.Date)
                break;

            string[] fileLines;
            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs))
            {
                fileLines = FilterLines(sr, levels, since).TakeLast(maxLines).ToArray();
            }

            if (fileLines.Length == 0) continue;

            chunks.Add(fileLines);
            totalLines += fileLines.Length;

            if (totalLines >= maxLines) break;
        }

        IEnumerable<string> allLines = chunks.AsEnumerable().Reverse().SelectMany(c => c);

        var skip = Math.Max(0, totalLines - maxLines);
        return allLines.Skip(skip).ToArray();
    }

    /// <summary>Дата из имени файла app_yyyy-MM-dd.log; false если имя не распознано.</summary>
    public static bool TryGetFileDate(string file, out DateTime date)
    {
        var name = Path.GetFileNameWithoutExtension(file);
        var datePart = name.StartsWith(LogFileNamePrefix, StringComparison.Ordinal)
            ? name[LogFileNamePrefix.Length..]
            : name;

        return DateTime.TryParseExact(datePart, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    /// <summary>
    /// Удаляет дневные файлы app_*.log с датой в имени строго раньше <paramref name="cutoffDate"/>.
    /// Файлы с нераспознанной датой не трогает. Возвращает количество удалённых.
    /// </summary>
    public static int DeleteFilesOlderThan(string logsDir, DateTime cutoffDate)
    {
        if (string.IsNullOrEmpty(logsDir) || !Directory.Exists(logsDir)) return 0;

        var deleted = 0;
        foreach (var file in Directory.EnumerateFiles(logsDir, LogFilePattern))
        {
            if (TryGetFileDate(file, out var fileDate) && fileDate < cutoffDate.Date)
            {
                File.Delete(file);
                deleted++;
            }
        }

        return deleted;
    }

    /// <summary>Уровень записи: первый известный токен в начале строки.</summary>
    static string ExtractLevel(string firstLine)
    {
        var tokens = firstLine.Split(TokenSeparators, StringSplitOptions.RemoveEmptyEntries);

        foreach (var token in tokens.Take(8))
        {
            var level = TryNormalizeLevel(token);
            if (level is not null) return level;
        }

        return "";
    }

    static List<string> EntryLinesIfPass(List<string> entry, DateTime timestamp, string level,
        IReadOnlyCollection<string>? levels, DateTime? since)
    {
        if (entry.Count == 0) return [];

        var levelOk = levels is null || (level.Length > 0 && levels.Contains(level));
        var timeOk = since is null || timestamp >= since;

        return levelOk && timeOk ? entry : [];
    }

    static DateTime ParseTimestamp(string value)
    {
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto))
            return dto.LocalDateTime;

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;

        return default;
    }

    static string? TryNormalizeLevel(string token) => token.ToUpperInvariant() switch
    {
        "TRACE" or "TRCE" or "VERBOSE" => "TRACE",
        "DEBUG" or "DBUG" or "DBG" => "DEBUG",
        "INFO" or "INFORMATION" or "INFR" => "INFO",
        "WARN" or "WARNING" or "WRN" => "WARN",
        "ERROR" or "EROR" or "ERR" => "ERROR",
        "CRITICAL" or "CRIT" or "FATAL" or "FTL" => "CRITICAL",
        _ => null,
    };
}
