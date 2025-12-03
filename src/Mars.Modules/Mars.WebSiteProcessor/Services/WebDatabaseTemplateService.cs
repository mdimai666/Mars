using Mars.Host.Data.Contexts;
using Mars.Host.Shared.Hubs;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Managers.Extensions;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite.Interfaces;
using Mars.Host.Shared.WebSite.Models;
using Mars.Host.WebSite.SourceProviders;
using Mars.Shared.Options;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebSiteProcessor.Services;

public class WebDatabaseTemplateService : IWebTemplateService
{
    WebSiteTemplate? _template;
    Debouncer _debouncer;
    MarsAppFront _appFront;
    IMarsDbContextFactory _marsDbContextFactory;

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
    readonly IServiceProvider _rootServiceProvider;
    readonly IHubContext<ChatHub> _hub;
    readonly IMemoryCache _memoryCache;

    public event EventHandler? OnFileUpdated;

    public WebDatabaseTemplateService(IServiceProvider rootServiceProvider,
        IHubContext<ChatHub> hub, MarsAppFront appFront, IEventManager eventManager)
    {
        var af = appFront.Configuration;
        _appFront = appFront;
        _marsDbContextFactory = rootServiceProvider.GetRequiredService<IMarsDbContextFactory>();

        //IWebFilesService webFilesService = rootServiceProvider.GetRequiredService<IWebFilesService>();

        //var see = nameof(Microsoft.AspNetCore.Mvc.HotReload.HotReloadService);

        _rootServiceProvider = rootServiceProvider;
        _hub = hub;
        _memoryCache = rootServiceProvider.GetRequiredService<IMemoryCache>();

        {
            string[] eventTypes = WebTemplateDatabaseSource.activeTypeNames;

            foreach (var eventType in eventTypes)
            {
                eventManager.AddEventListener(eventManager.Defaults.PostAdd(eventType), payload =>
                {
                    UpdateFile("", WatcherChangeTypes.Created);
                    OnFileUpdated?.Invoke(this, payload);
                    //hub.Clients.All.SendAsync("reload");//refreshcss
                });
                eventManager.AddEventListener(eventManager.Defaults.PostUpdate(eventType), payload =>
                {
                    UpdateFile("", WatcherChangeTypes.Changed);
                    OnFileUpdated?.Invoke(this, payload);
                    //hub.Clients.All.SendAsync("reload");//refreshcss
                });
                eventManager.AddEventListener(eventManager.Defaults.PostDelete(eventType), payload =>
                {
                    UpdateFile("", WatcherChangeTypes.Deleted);
                    OnFileUpdated?.Invoke(this, payload);
                    //hub.Clients.All.SendAsync("reload");//refreshcss
                });
            }
            eventManager.AddEventListener(eventManager.Defaults.OptionUpdate(nameof(FrontOptions)), payload =>
            {
                UpdateFile("", WatcherChangeTypes.Changed);
                OnFileUpdated?.Invoke(this, payload);
                //hub.Clients.All.SendAsync("reload");//refreshcss

            });
        }

        _debouncer = new Debouncer(200);

        TryScanSite();
    }

    public void ScanSite()
    {
        using var scope = _rootServiceProvider.CreateScope();
        var optionService = scope.ServiceProvider.GetRequiredService<IOptionService>();
        var templateSource = new WebTemplateDatabaseSource(optionService, _appFront, _marsDbContextFactory.CreateInstance);
        _template = new WebSiteTemplate(templateSource.ReadParts());
        //string indexContent = _template.Page404.Content;
    }

    void TryScanSite()
    {
        try
        {
            //_template.ScanSite();
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
        ScanSite();

        _hub.Clients.All.SendAsync("reload");//refreshcss

    }

    public void ClearCache()
    {
        if (_memoryCache is MemoryCache mc)
        {
            mc.Clear();
        }
    }

}
