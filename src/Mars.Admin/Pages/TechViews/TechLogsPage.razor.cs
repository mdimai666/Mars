using System.Globalization;
using System.Text.RegularExpressions;
using Mars.Core.Extensions;
using MarsCodeEditor2;
using Microsoft.JSInterop;

namespace Mars.Admin.Pages.TechViews;

public partial class TechLogsPage : IDisposable
{
    bool Busy = true;
    string text = "";
    string? error;
    List<LogEntry> entries = [];

    CodeEditor2? editor;

    bool _monacoView;
    string _filter = "all";
    readonly HashSet<string> _levelsMulti = ["WARN", "ERROR", "CRITICAL"];
    string _query = "";
    bool _dateMode;
    string _period = "1d";
    DateTime? _from;
    DateTime? _to;

    bool _live;
    CancellationTokenSource? _liveCts;

    LogEntry? _entry;

    void ShowEntry(LogEntry entry) => _entry = entry;

    void CloseEntry() => _entry = null;

    record Chip(string Id, string Label);

    // табличный вид: чипы из дизайна, выбор одного
    static readonly Chip[] TableChips =
    [
        new("all", "All"),
        new("ERROR", "Errors"),
        new("WARN", "Warnings"),
        new("INFO", "Info"),
        new("DEBUG", "Debug"),
    ];

    // monaco-вид: полный набор уровней, выбор нескольких (как на старой странице Debug)
    static readonly Chip[] MonacoChips =
    [
        new("TRACE", "Trace"),
        new("DEBUG", "Debug"),
        new("INFO", "Info"),
        new("WARN", "Warn"),
        new("ERROR", "Error"),
        new("CRITICAL", "Critical"),
    ];

    static readonly Chip[] PeriodChips =
    [
        new("", "всё время"),
        new("1h", "за час"),
        new("6h", "за 6 часов"),
        new("1d", "за день"),
        new("7d", "за неделю"),
        new("30d", "за месяц"),
    ];

    Chip[] CurrentChips => _monacoView ? MonacoChips : TableChips;

    bool IsChipActive(string id) => _monacoView ? _levelsMulti.Contains(id) : _filter == id;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _ = Load();
    }

    async Task Load(bool silent = false)
    {
        if (!silent)
        {
            Busy = true;
            StateHasChanged();
        }

        var res = await client.AppDebug.GetLogs(1000, SelectedLevels(),
            _dateMode ? null : _period,
            _dateMode ? _from?.ToString("yyyy-MM-dd") : null,
            _dateMode ? _to?.ToString("yyyy-MM-dd") : null);

        if (res.Ok)
        {
            error = null;
            text = res.Data;
            entries = LogEntryParser.Parse(text);
        }
        else
        {
            error = res.Message;
            text = "";
            entries = [];
        }

        Busy = false;
        StateHasChanged();
    }

    IReadOnlyCollection<string>? SelectedLevels() =>
        _monacoView
            ? (_levelsMulti.Count > 0 ? [.. _levelsMulti] : null)
            : (_filter == "all" ? null : [_filter]);

    void OnChipClick(string id)
    {
        if (_monacoView)
        {
            if (!_levelsMulti.Remove(id)) _levelsMulti.Add(id);
        }
        else
        {
            _filter = id;
        }

        _ = Load();
    }

    void OnViewChanged()
    {
        if (_monacoView)
        {
            _levelsMulti.Clear();
            if (_filter != "all") _levelsMulti.Add(_filter);
            else foreach (var level in new[] { "WARN", "ERROR", "CRITICAL" }) _levelsMulti.Add(level);
        }
        else
        {
            _filter = _levelsMulti.Count == 1 ? _levelsMulti.First() : "all";
        }

        _ = Load();
    }

    void OnDateChanged() => _ = Load();

    void OnPeriodClick(string id)
    {
        _period = id;
        _ = Load();
    }

    void ToggleDateMode()
    {
        _dateMode = !_dateMode;
        _ = Load();
    }

    void ResetDates()
    {
        _from = null;
        _to = null;
        _ = Load();
    }

    // лог-файл хронологический; в таблице свежие записи сверху
    IEnumerable<LogEntry> FilteredEntries()
    {
        var source = string.IsNullOrEmpty(_query)
            ? entries
            : entries.Where(e =>
                e.Message.Contains(_query, StringComparison.OrdinalIgnoreCase) ||
                e.Source.Contains(_query, StringComparison.OrdinalIgnoreCase));

        return source.Reverse();
    }

    void ToggleLive()
    {
        _live = !_live;

        if (_live)
        {
            _liveCts = new CancellationTokenSource();
            _ = LiveLoop(_liveCts.Token);
        }
        else
        {
            _liveCts?.Cancel();
            _liveCts = null;
        }
    }

    async Task LiveLoop(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
                await InvokeAsync(() => Load(silent: true));
            }
        }
        catch (OperationCanceledException) { }
    }

    async Task Export()
    {
        if (string.IsNullOrEmpty(text)) return;
        var name = $"logs_{DateTime.Now:yyyy-MM-dd_HHmm}.log";
        await JS.InvokeVoidAsync("marsDownloadText", name, text);
    }

    void OnEditorInit()
    {
        _ = ScrollDown();
    }

    async Task ScrollDown()
    {
        WaitHelper.WaitForNotNull(() => editor, 1000);
        if (editor is null) return;

        var sh = await editor.Monaco.GetScrollHeight();
        await editor.Monaco.SetScrollTop((int)(sh - 1500));
    }

    public void Dispose()
    {
        _liveCts?.Cancel();
        _liveCts?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>Запись лога для табличного вида.</summary>
    public record LogEntry(DateTime Ts, string Level, string Source, string Message);

    /// <summary>
    /// Разбор текста лога (формат NReco.Logging.File "ts\tLEVEL\t[Category]\t[EventId]\tсообщение",
    /// понимается и старый формат "ts [7] WARN Logger[0] - сообщение"). Многострочные записи
    /// (stack trace) склеиваются в Message.
    /// </summary>
    static partial class LogEntryParser
    {
        [GeneratedRegex(@"^(?<ts>\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}:\d{2}(?:[.,]\d+)?(?:Z|[+-]\d{1,2}:?\d{2})?)")]
        private static partial Regex EntryStartRegex();

        [GeneratedRegex(@"^\[\d*\]$")]
        private static partial Regex EventIdRegex();

        static readonly Dictionary<string, string> LevelMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["TRACE"] = "TRACE", ["TRCE"] = "TRACE", ["VERBOSE"] = "TRACE",
            ["DEBUG"] = "DEBUG", ["DBUG"] = "DEBUG", ["DBG"] = "DEBUG",
            ["INFO"] = "INFO", ["INFORMATION"] = "INFO", ["INFR"] = "INFO",
            ["WARN"] = "WARN", ["WARNING"] = "WARN", ["WRN"] = "WARN",
            ["ERROR"] = "ERROR", ["EROR"] = "ERROR", ["ERR"] = "ERROR",
            ["CRITICAL"] = "CRITICAL", ["CRIT"] = "CRITICAL", ["FATAL"] = "CRITICAL", ["FTL"] = "CRITICAL",
        };

        public static List<LogEntry> Parse(string text)
        {
            var result = new List<LogEntry>();
            if (string.IsNullOrEmpty(text)) return result;

            foreach (var line in text.Split('\n'))
            {
                var match = EntryStartRegex().Match(line);
                if (match.Success)
                {
                    result.Add(ParseFirstLine(match.Groups["ts"].Value, line.TrimEnd('\r')));
                }
                else if (result.Count > 0)
                {
                    var last = result[^1];
                    result[^1] = last with { Message = last.Message + "\n" + line.TrimEnd('\r') };
                }
            }

            return result;
        }

        static LogEntry ParseFirstLine(string tsRaw, string line)
        {
            var ts = ParseTimestamp(tsRaw);
            var rest = line[tsRaw.Length..];

            // таб-формат NReco: ts \t LEVEL \t [Category] \t [EventId] \t message
            if (rest.Contains('\t'))
            {
                var parts = rest.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                var tabLevel = NormalizeLevel(parts.ElementAtOrDefault(0) ?? "");
                var tabSource = (parts.ElementAtOrDefault(1) ?? "").Trim('[', ']');
                var skip = parts.Length > 2 && EventIdRegex().IsMatch(parts[2]) ? 3 : 2;
                var tabMessage = string.Join('\t', parts.Skip(skip));
                return new LogEntry(ts, tabLevel, tabSource, tabMessage);
            }

            // старый формат: " [7] WARN Logger[0] - сообщение"
            var tokens = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var levelToken = tokens.Take(8).FirstOrDefault(t => LevelMap.ContainsKey(t));
            var dashIndex = rest.IndexOf(" - ", StringComparison.Ordinal);
            var legacyMessage = dashIndex >= 0 ? rest[(dashIndex + 3)..] : rest.Trim();
            var legacySource = tokens.LastOrDefault(t => t.EndsWith(']') && t.Contains('[') && !t.StartsWith('[')) ?? "";
            return new LogEntry(ts, NormalizeLevel(levelToken ?? ""), legacySource, legacyMessage);
        }

        static string NormalizeLevel(string token) => LevelMap.GetValueOrDefault(token, token.ToUpperInvariant());

        static DateTime ParseTimestamp(string value)
        {
            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto))
                return dto.LocalDateTime;

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt;

            return default;
        }
    }
}
