using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Mars.Core.Models;
using Mars.Server.Abstractions.Models;
using Mars.SiteEngine.Abstractions.Models;
using Mars.SiteEngine.Contracts.Options;
using Mars.SiteEngine.Abstractions.Services;
using Mars.Core.TemplateEngine;
using Mars.SiteEngine.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Mars.SiteEngine.Services;

public class WebRenderEngineLocator : IWebRenderEngineLocator
{
    record CacheEntry(MarsAppFront App, IWebRenderEngine Engine, StaticFileMiddleware? StaticFiles, FrontItem Snapshot);

    static readonly object StaticNotServedKey = new();
    static readonly RequestDelegate StaticNotServed = context =>
    {
        context.Items[StaticNotServedKey] = true;
        return Task.CompletedTask;
    };

    // стабильные ассеты кешируются надолго; css/js/html и прочее — ревалидация
    // через ETag/Last-Modified, которые проставляет StaticFileMiddleware
    static readonly HashSet<string> LongTermCacheExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".webp", ".avif", ".svg", ".bmp", ".ico",
        ".woff", ".woff2", ".ttf", ".otf", ".eot",
        ".mp4", ".webm", ".mp3", ".ogg", ".wav",
        ".wasm",
    };

    const string LongTermCacheControl = "public, max-age=2592000";
    const string RevalidateCacheControl = "no-cache";

    readonly IFrontManager frontManager;
    readonly IServiceProvider rootServices;
    readonly Dictionary<string, IWebRenderEngineFactory> factories;
    readonly ConcurrentDictionary<string, CacheEntry> cache = new(StringComparer.OrdinalIgnoreCase);
    readonly FileExtensionContentTypeProvider contentTypeProvider = CreateContentTypeProvider();
    readonly object buildLock = new();

    static FileExtensionContentTypeProvider CreateContentTypeProvider()
    {
        var provider = new FileExtensionContentTypeProvider();
        provider.Mappings.TryAdd(".wasm", "application/wasm");
        return provider;
    }

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
        var front = frontManager.FindBySlug(slug);
        if (front is null) return null;

        return GetOrCreate(front).App;
    }

    public MarsAppFront? TryGetAppFrontBySlug(string slug)
    {
        return cache.TryGetValue(slug, out var entry) ? entry.App : null;
    }

    public async Task<bool> TryServeStaticFileAsync(HttpContext context, MarsAppFront appFront)
    {
        var slug = appFront.Front?.Slug;
        if (slug is null || !cache.TryGetValue(slug, out var entry) || entry.StaticFiles is null)
            return false;

        context.Items.Remove(StaticNotServedKey);
        await entry.StaticFiles.Invoke(context);
        return !context.Items.ContainsKey(StaticNotServedKey);
    }

    void OnFrontsChanged()
    {
        foreach (var (slug, entry) in cache)
        {
            // FindBySlug видит и публичные фронты, и специальный админ-фронт —
            // иначе админ-фронт (его нет в Fronts) вытеснялся бы из кеша при каждом сохранении опции.
            var front = frontManager.FindBySlug(slug);

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
        };

        var appFront = new MarsAppFront
        {
            Configuration = configuration,
            Front = front,
        };

        var engine = factory.Create(appFront, rootServices);
        appFront.Features.Set(engine);

        var wwwrootPath = Path.Combine(configuration.Path, "wwwroot");
        StaticFileMiddleware? staticFiles = Directory.Exists(wwwrootPath)
            ? BuildStaticFiles(front, wwwrootPath)
            : null;

        return new CacheEntry(appFront, engine, staticFiles, front);
    }

    StaticFileMiddleware BuildStaticFiles(FrontItem front, string wwwrootPath)
    {
        var options = new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(wwwrootPath),
            RequestPath = front.Url,
            ContentTypeProvider = contentTypeProvider,
            OnPrepareResponse = ctx =>
            {
                ctx.Context.Response.Headers.CacheControl =
                    LongTermCacheExtensions.Contains(Path.GetExtension(ctx.File.Name))
                        ? LongTermCacheControl
                        : RevalidateCacheControl;
            },
        };

        return new StaticFileMiddleware(
            StaticNotServed,
            rootServices.GetRequiredService<IWebHostEnvironment>(),
            Microsoft.Extensions.Options.Options.Create(options),
            rootServices.GetRequiredService<ILoggerFactory>());
    }
}
