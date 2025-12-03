using Mars.Host.Data.Contexts;
using Mars.Host.Shared.Hubs;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Managers.Extensions;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite.Interfaces;
using Mars.Host.Shared.WebSite.Models;
using Mars.Host.Shared.WebSite.SourceProviders;
using Mars.Host.WebSite.SourceProviders;
using Mars.Shared.Options;
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
    MarsAppFront _appFront;

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

    readonly IHostEnvironment env;
    bool IsDevelopment;

    public WebTemplateService(IServiceProvider rootServiceProvider,
        IHubContext<ChatHub> hub, MarsAppFront appFront)
    {
        //var af = configuration.GetRequiredSection("AppFront").Get<AppFrontSettingsCfg>();
        var af = appFront.Configuration;
        _appFront = appFront;
        Path = af.Path;
        bool enableWatcher = true;

        //IWebFilesService webFilesService = rootServiceProvider.GetRequiredService<IWebFilesService>();
        //var see = nameof(Microsoft.AspNetCore.Mvc.HotReload.HotReloadService);

        _rootServiceProvider = rootServiceProvider;
        _hub = hub;
        _memoryCache = rootServiceProvider.GetRequiredService<IMemoryCache>();

        env = rootServiceProvider.GetRequiredService<IHostEnvironment>();
        IsDevelopment = env.IsDevelopment();

        if (enableWatcher)
        {
            _watcher = new FileSystemWatcher(Path)
            {
                NotifyFilter = NotifyFilters.DirectoryName
                                     | NotifyFilters.FileName
                                     | NotifyFilters.LastWrite
            };

            _watcher.Changed += OnChanged;
            _watcher.Created += OnCreated;
            _watcher.Deleted += OnDeleted;
            _watcher.Renamed += OnRenamed;
            _watcher.Error += OnError;

            _watcher.Disposed += Watcher_Disposed;

            _watcher.Filters.Add("*.hbs");
            _watcher.Filters.Add("*.css");
            _watcher.Filters.Add("*.js");
            _watcher.Filters.Add("*.resx");
            _watcher.IncludeSubdirectories = true;
            _watcher.EnableRaisingEvents = true;
            //Console.WriteLine("SetWatcher");
        }

        {
            var eventManager = rootServiceProvider.GetRequiredService<IEventManager>();

            string[] eventTypes = WebTemplateDatabaseSource.activeTypeNames;

            foreach (var eventType in eventTypes)
            {
                eventManager.AddEventListener(eventManager.Defaults.PostAdd(eventType), payload =>
                {
                    UpdateFile("x", WatcherChangeTypes.Created);
                    OnFileUpdated?.Invoke(this, payload);
                    //hub.Clients.All.SendAsync("reload");//refreshcss
                });
                eventManager.AddEventListener(eventManager.Defaults.PostUpdate(eventType), payload =>
                {
                    UpdateFile("x", WatcherChangeTypes.Changed);
                    OnFileUpdated?.Invoke(this, payload);
                    //hub.Clients.All.SendAsync("reload");//refreshcss
                });
                eventManager.AddEventListener(eventManager.Defaults.PostDelete(eventType), payload =>
                {
                    UpdateFile("x", WatcherChangeTypes.Deleted);
                    OnFileUpdated?.Invoke(this, payload);
                    //hub.Clients.All.SendAsync("reload");//refreshcss
                });
            }
            eventManager.AddEventListener(eventManager.Defaults.OptionUpdate(nameof(FrontOptions)), payload =>
            {
                UpdateFile("x", WatcherChangeTypes.Changed);
                OnFileUpdated?.Invoke(this, payload);
                //hub.Clients.All.SendAsync("reload");//refreshcss

            });
        }

        _debouncer = new Debouncer(200);

        TryScanSite();
    }

    public void ScanSite()
    {
        var wfs = new WebFilesReadFilesystemService();
        var templateSource = new WebTemplateFilesystemSource(Path, wfs);
        var optionService = _rootServiceProvider.GetRequiredService<IOptionService>();
        var eff = _rootServiceProvider.GetRequiredService<IMarsDbContextFactory>();

        if (eff is not null)
        {

            var dbTemplateSource = new WebTemplateDatabaseSource(optionService, _appFront, () => eff?.CreateInstance() ?? null!);

            var dbParts = dbTemplateSource.ReadParts();
            dbParts = dbParts.Where(x => x.Name != "_root.hbs");

            _template = new WebSiteTemplate(templateSource.ReadParts().Concat(dbParts));
        }
        else
        {
            _template = new WebSiteTemplate(templateSource.ReadParts());
        }
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

    void Watcher_Disposed(object? sender, EventArgs e)
    {
        Console.WriteLine("Watcher_Disposed");
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
    void _updateFile(string path, WatcherChangeTypes changeType)
    {
        if (path == "x") //updated parts from Datadase
        {
            ScanSite();
            _hub.Clients.All.SendAsync("reload");
        }

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
            if (changeType == WatcherChangeTypes.Deleted)
            {
                Console.WriteLine($"Deleted: {path}");
                //_template.DeleteFile(path);
                ScanSite();
            }
            else if (changeType == WatcherChangeTypes.Created)
            {
                Console.WriteLine($"Created: {path}");
                //_template.CreatedFile(path);
                ScanSite();
            }
            else if (changeType == WatcherChangeTypes.Changed)
            {
                Console.WriteLine($"Changed: {path}");
                //_template.ChangedFile(path);
                ScanSite();
            }
            else
            {
                throw new NotImplementedException();
            }
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
        if (_memoryCache is MemoryCache mc)
        {
            if (IsDevelopment == false)
            {
                mc.Clear();
            }
        }
    }

}
