using Mars.Core.Extensions;
using MarsCodeEditor2;

namespace Mars.Admin.Builder.DebugViews;

public partial class DebugPage
{
    bool Busy = true;
    string text = "";
    string? error;

    CodeEditor2? editor1;

    readonly string[] levelOptions = ["TRACE", "DEBUG", "INFO", "WARN", "ERROR", "CRITICAL"];
    IEnumerable<string> selectedLevels = ["WARN", "ERROR", "CRITICAL"];

    // цвета согласованы с темой logview Monaco-редактора логов
    static readonly Dictionary<string, string> LevelColors = new()
    {
        ["TRACE"] = "#808080",
        ["DEBUG"] = "#008800",
        ["INFO"] = "#4b71ca",
        ["WARN"] = "#FFA500",
        ["ERROR"] = "#dc3545",
        ["CRITICAL"] = "#a10000",
    };

    static string LevelColor(string level) => LevelColors.GetValueOrDefault(level, "#808080");

    readonly KeyValuePair<string, string>[] periodOptions =
    [
        new("", "всё время"),
        new("1h", "за час"),
        new("6h", "за 6 часов"),
        new("1d", "за день"),
        new("7d", "за неделю"),
        new("30d", "за месяц"),
    ];
    string period = "1d";

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Load();
    }

    async void Load()
    {
        Busy = true;
        StateHasChanged();

        var res = await client.AppDebug.GetLogs(1000, [.. selectedLevels], period);

        if (res.Ok)
        {
            error = null;
            text = res.Data;
        }
        else
        {
            error = res.Message;
        }

        Busy = false;
        StateHasChanged();
    }

    void SetPeriod(string value)
    {
        if (period == value) return;
        period = value;
        Load();
    }

    void OnInit()
    {
        ScrollDown();
    }

    async void ScrollDown()
    {
        WaitHelper.WaitForNotNull(() => editor1, 1000);

        var sh = await editor1.Monaco.GetScrollHeight();
        await editor1.Monaco.SetScrollTop((int)(sh - 1500));
    }
}
