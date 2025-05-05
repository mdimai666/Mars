using MarsEditors;
using Microsoft.AspNetCore.Components;

namespace AppAdmin.Builder.DebugViews;

public partial class DebugPage
{
    [Parameter] public string FILENAME { get; set; } = default!;

    bool Busy = true;
    string text = "";
    string? error;

    MarsCodeEditor? editor1;
    string? prevFileName;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (prevFileName is null || prevFileName != FILENAME)
        {
            Load();
        }
        prevFileName = FILENAME;
    }

    IReadOnlyCollection<string>? logfiles;


    async void Load()
    {
        Busy = true;
        StateHasChanged();

        logfiles ??= await client.AppDebug.LogFiles();

        var fileName = string.IsNullOrEmpty(FILENAME) ? logfiles.First() : FILENAME;

        var res = await client.AppDebug.GetLogs(fileName, 300);

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

    async void LoadLogFiles()
    {
        logfiles = await client.AppDebug.LogFiles();

        StateHasChanged();
    }

    async void IFrameLoaded()
    {
        await Task.Delay(500);
        _ = editor1?.SendToMonaco("d_scrollDown", "0");
    }

    void OnChangeLogFile(string file)
    {
        var u = $"/dev/builder/debug/{file}";
        navigationManager.NavigateTo(u);
    }
}
