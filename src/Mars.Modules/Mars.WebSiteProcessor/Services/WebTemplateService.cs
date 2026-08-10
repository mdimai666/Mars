using Mars.Host.Shared.Hubs;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite.Interfaces;
using Mars.Host.Shared.WebSite.Models;
using Mars.Host.Shared.WebSite.SourceProviders;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mars.WebSiteProcessor.Services;

public class WebTemplateService : IWebTemplateService
{
    public string Path { get; init; }

    FileSystemWatcher? _watcher;
    WebSiteTemplate _template = default!;
    Debouncer _debouncer;

    public event EventHandler? OnFileUpdated;

    public WebSiteTemplate Template
    {
        get
        {
            //if (_template.Parts.Count == 0) _template.ScanSite().Wait();
            if (_template is null || _template.Parts.Count == 0) ScanSite();
            return _template!;
        }
        set => _template = value;
    }

    //bool lastErrored = false;
    private readonly IServiceProvider _rootServiceProvider;
    private readonly IHubContext<ChatHub> _hub;
    private readonly IMemoryCache _memoryCache;

    public WebTemplateService(IServiceProvider rootServiceProvider,
        IHubContext<ChatHub> hub, MarsAppFront appFront)
    {
        var af = appFront.Configuration;
        Path = af.Path;

        _rootServiceProvider = rootServiceProvider;
        _hub = hub;
        _memoryCache = rootServiceProvider.GetRequiredService<IMemoryCache>();

        SetupWatcher();

        // Debounce: при выгрузке множества файлов перечитывать шаблон один раз, а не на каждое событие
        _debouncer = new Debouncer(200);

        TryScanSite();
    }

    void SetupWatcher()
    {
        if (!Directory.Exists(Path))
            return;

        try
        {
            _watcher = new FileSystemWatcher(Path)
            {
                NotifyFilter = NotifyFilters.DirectoryName
                                     | NotifyFilters.FileName
                                     | NotifyFilters.LastWrite,
                // чтобы события не терялись при массовой выгрузке файлов
                InternalBufferSize = 64 * 1024,
            };

            _watcher.Changed += OnChanged;
            _watcher.Created += OnCreated;
            _watcher.Deleted += OnDeleted;
            _watcher.Renamed += OnRenamed;
            _watcher.Error += OnError;

            _watcher.Filters.Add("*.hbs");
            _watcher.Filters.Add("*.css");
            _watcher.Filters.Add("*.js");
            _watcher.Filters.Add("*.resx");
            _watcher.IncludeSubdirectories = true;
            _watcher.EnableRaisingEvents = true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"WebTemplateService: watcher init failed for '{Path}': {ex.Message}");
        }
    }

    public void ScanSite()
    {
        var wfs = new WebFilesReadFilesystemService();
        var templateSource = new WebTemplateFilesystemSource(Path, wfs);

        _template = new WebSiteTemplate(templateSource.ReadParts());
    }

    void TryScanSite()
    {
        try
        {
            //_template.ScanSite().Wait();
            ScanSite();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            //lastErrored = true;
        }
    }

    void OnChanged(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType != WatcherChangeTypes.Changed)
        {
            return;
        }
        //Console.WriteLine($"Changed: {e.FullPath}");
        UpdateFile(e.FullPath, e.ChangeType);
    }

    void OnCreated(object sender, FileSystemEventArgs e)
    {
        //Console.WriteLine($"Created: {e.FullPath}");
        UpdateFile(e.FullPath, e.ChangeType);
    }

    void OnDeleted(object sender, FileSystemEventArgs e)
    {
        //Console.WriteLine($"Deleted: {e.FullPath}");
        UpdateFile(e.FullPath, e.ChangeType);
    }

    void OnRenamed(object sender, RenamedEventArgs e)
    {
        //Console.WriteLine($"Renamed:");
        //Console.WriteLine($"    Old: {e.OldFullPath}");
        //Console.WriteLine($"    New: {e.FullPath}");
        //UpdateFile(e.FullPath, e.ChangeType);

        UpdateFile(e.OldFullPath, WatcherChangeTypes.Deleted);
        UpdateFile(e.FullPath, WatcherChangeTypes.Created);
    }

    void OnError(object sender, ErrorEventArgs e) =>
            PrintException(e.GetException());

    void PrintException(Exception? ex)
    {
        if (ex != null)
        {
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine("Stacktrace:");
            Console.WriteLine(ex.StackTrace);
            Console.WriteLine();
            PrintException(ex.InnerException);
        }
    }

    void UpdateFile(string path, WatcherChangeTypes changeType)
    {
        _debouncer.Debouce(() => { _updateFile(path, changeType); });
    }

    public void NotifyFileChanged(string fullPath)
    {
        // сразу, без дебаунса: запись уже завершена (это не поток файловых событий),
        // а превью и кеш рендера должны обновиться детерминированно
        _updateFile(fullPath, WatcherChangeTypes.Changed);
    }
    void _updateFile(string path, WatcherChangeTypes changeType)
    {
        string ext = System.IO.Path.GetExtension(path);

        if (ext == ".css")
        {
            string filename = System.IO.Path.GetFileName(path);
            _hub.Clients.All.SendAsync("refreshcss", filename);
            return;
        }
        else if (ext == ".js")
        {
            _hub.Clients.All.SendAsync("reload");
            return;
        }
        else if (ext == ".hbs")
        {
            Console.WriteLine($"Front file {changeType}: {path}");
            // TryScanSite: если шаблон в момент перечитывания битый, остаётся предыдущая версия
            TryScanSite();
        }
        else if (ext == ".resx")
        {
            IAppFrontLocalizer? afl = _rootServiceProvider.GetService<IAppFrontLocalizer>();
            if (afl != null)
            {
                afl.Refresh();
            }
        }

        OnFileUpdated?.Invoke(path, EventArgs.Empty);
        _hub.Clients.All.SendAsync("reload");//refreshcss

    }

    public void ClearCache()
    {
        // иначе после правки файлов скомпилированные шаблоны висели бы в кеше до 30 минут
        if (_memoryCache is MemoryCache mc)
        {
            mc.Clear();
        }
    }

}
