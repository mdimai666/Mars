using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Mars.Core.Models;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.TemplateEngine;
using Mars.Shared.Options;
using Mars.WebSiteProcessor.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace Mars.WebSiteProcessor.Services;

public class WebRenderEngineLocator : IWebRenderEngineLocator
{
    record CacheEntry(MarsAppFront App, IWebRenderEngine Engine, PhysicalFileProvider? WwwRoot, FrontItem Snapshot);

    readonly IFrontManager frontManager;
    readonly IServiceProvider rootServices;
    readonly Dictionary<string, IWebRenderEngineFactory> factories;
    readonly ConcurrentDictionary<string, CacheEntry> cache = new(StringComparer.OrdinalIgnoreCase);
    readonly FileExtensionContentTypeProvider contentTypeProvider = new();
    readonly object buildLock = new();

    public WebRenderEngineLocator(
        IFrontManager frontManager,
        IEnumerable<IWebRenderEngineFactory> factories,
        IServiceProvider rootServices)
    {
        this.frontManager = frontManager;
        this.rootServices = rootServices;
        this.factories = factories.ToDictionary(f => f.Id, f => f, StringComparer.OrdinalIgnoreCase);

        frontManager.Changed += OnFrontsChanged;
    }

    public IReadOnlyCollection<EngineMetadata> GetAvailableEngines()
    {
        return factories.Values.Select(factory =>
        {
            var displayAttribute = factory.GetType().GetCustomAttribute<DisplayAttribute>();
            string name = displayAttribute?.GetName() ?? factory.GetType().Name;
            string description = displayAttribute?.GetDescription() ?? string.Empty;
            return new EngineMetadata(factory.Id, name, description);
        }).ToList();
    }

    public MarsAppFront? GetAppFrontForUrl(string url)
    {
        var front = frontManager.GetFrontForUrl(url);
        if (front is null) return null;

        return GetOrCreate(front).App;
    }

    public MarsAppFront? GetAppFrontBySlug(string slug)
    {
        var front = frontManager.Fronts.FirstOrDefault(s => string.Equals(s.Slug, slug, StringComparison.OrdinalIgnoreCase));
        if (front is null) return null;

        return GetOrCreate(front).App;
    }

    public async Task<bool> TryServeStaticFileAsync(HttpContext context, MarsAppFront appFront)
    {
        //TODO: Выглядит костыльно и без кеш заголовка.
        var slug = appFront.Front?.Slug;
        if (slug is null || !cache.TryGetValue(slug, out var entry) || entry.WwwRoot is null)
            return false;

        var path = context.Request.Path;
        if (!string.IsNullOrEmpty(appFront.Configuration.Url))
        {
            if (!path.StartsWithSegments(appFront.Configuration.Url, out path))
                return false;
        }

        var fileInfo = entry.WwwRoot.GetFileInfo(path.Value ?? "");
        if (!fileInfo.Exists || fileInfo.IsDirectory)
            return false;

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = contentTypeProvider.TryGetContentType(fileInfo.Name, out var contentType)
            ? contentType
            : "application/octet-stream";

        await using var stream = fileInfo.CreateReadStream();
        await stream.CopyToAsync(context.Response.Body, context.RequestAborted);

        return true;
    }

    void OnFrontsChanged()
    {
        foreach (var (slug, entry) in cache)
        {
            var front = frontManager.Fronts.FirstOrDefault(s => string.Equals(s.Slug, slug, StringComparison.OrdinalIgnoreCase));

            if (front is null || FrontChanged(entry.Snapshot, front))
            {
                cache.TryRemove(slug, out _);
            }
        }
    }

    static bool FrontChanged(FrontItem a, FrontItem b)
        => a.Url != b.Url
        || a.Path != b.Path
        || a.EngineId != b.EngineId
        || a.Enabled != b.Enabled;

    CacheEntry GetOrCreate(FrontItem front)
    {
        if (cache.TryGetValue(front.Slug, out var existing))
            return existing;

        lock (buildLock)
        {
            if (cache.TryGetValue(front.Slug, out existing))
                return existing;

            var entry = Build(front);
            cache[front.Slug] = entry;
            return entry;
        }
    }

    CacheEntry Build(FrontItem front)
    {
        if (!factories.TryGetValue(front.EngineId, out var factory))
        {
            var available = string.Join(", ", factories.Keys);
            throw new NotSupportedException($"Рендер-движок '{front.EngineId}' (фронт '{front.Slug}') не найден. Доступны: {available}");
        }

        var configuration = new AppFrontSettingsCfg
        {
            Url = front.Url,
            Path = frontManager.ResolvePhysicalPath(front),
            Mode = AppFrontMode.HandlebarsTemplateStatic,
        };

        var appFront = new MarsAppFront
        {
            Configuration = configuration,
            Front = front,
        };

        var engine = factory.Create(appFront, rootServices);
        appFront.Features.Set(engine);

        var wwwrootPath = Path.Combine(configuration.Path, "wwwroot");
        PhysicalFileProvider? wwwRoot = Directory.Exists(wwwrootPath)
            ? new PhysicalFileProvider(wwwrootPath)
            : null;

        return new CacheEntry(appFront, engine, wwwRoot, front);
    }
}
