namespace MarsDocs.DevServer.Services;

public class MdWatcher : BackgroundService
{
    private readonly SseManager _sse;
    private readonly ILogger<MdWatcher> _logger;

    public MdWatcher(SseManager sse, ILogger<MdWatcher> logger)
    {
        _sse = sse;
        _logger = logger;
    }

    Debouncer debouncer = new(200);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var watcher = new FileSystemWatcher(Path.Combine("..", "..", "dev_docs"), "*.md")
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };

        watcher.Changed += (_, e) => debouncer.Debouce(() => UpdateFile(e));

        var tcs = new TaskCompletionSource();
        _ = stoppingToken.Register(() => { watcher.Dispose(); tcs.TrySetResult(); });

        return tcs.Task;
    }

    async void UpdateFile(FileSystemEventArgs e)
    {
        _logger.LogInformation($"📄 Markdown changed: {e.FullPath}");
        await _sse.BroadcastAsync($"reload;{NormalizeFilePath(e.FullPath)}");
    }

    string NormalizeFilePath(string filepath) => Path.GetRelativePath(Path.Combine("..", ".."), filepath).Replace('\\', '/');
}
