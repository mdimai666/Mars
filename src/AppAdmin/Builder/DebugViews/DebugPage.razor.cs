using Mars.Core.Extensions;
using MarsCodeEditor2;
using Microsoft.AspNetCore.Components;

namespace AppAdmin.Builder.DebugViews;

public partial class DebugPage
{
    [Parameter] public string FILENAME { get; set; } = default!;

    bool Busy = true;
    string text = "";
    string? error;

    CodeEditor2? editor1;
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

        var fileName = string.IsNullOrEmpty(FILENAME) ? logfiles.FirstOrDefault() : FILENAME;

        if (fileName is not null)
        {
            var res = await client.AppDebug.GetLogs(fileName, 1000);

            if (res.Ok)
            {
                error = null;
                text = res.Data;
            }
            else
            {
                error = res.Message;
            }
        }



        Busy = false;
        StateHasChanged();

    }

    void OnInit()
    {
        ScrollDown();
    }

    async void LoadLogFiles()
    {
        logfiles = await client.AppDebug.LogFiles();

        StateHasChanged();
    }

    async void ScrollDown()
    {
        WaitHelper.WaitForNotNull(() => editor1, 1000);

        var sh = await editor1.Monaco.GetScrollHeight();
        await editor1.Monaco.SetScrollTop((int)(sh - 1500));
    }

    void OnChangeLogFile(string file)
    {
        var u = $"/dev/builder/debug/{file}";
        navigationManager.NavigateTo(u);
    }
}
