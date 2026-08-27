using Mars.Core.Extensions;
using Mars.Host.Shared.WebSite.Scripts;
using Microsoft.Extensions.Caching.Memory;

namespace Mars.Host.WebSite.Scripts;

internal class SiteScriptsBuilder : ISiteScriptsBuilder
{
    private readonly string Id = $"SiteScriptsBuilder_{Guid.NewGuid()}";

    public const string PlaceHead = "head";
    public const string PlaceFooter = "footer";
    private readonly List<SiteAssetItem> _providers = [];
    private readonly IMemoryCache _memoryCache;

    public SiteScriptsBuilder(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public void RegisterProvider(string key, ISiteAssetPrivider provider, float order = 10f, bool placeInHead = false)
    {
        _providers.Add(new(key, provider, order, placeInHead ? PlaceHead : PlaceFooter));
    }

    public string HeadScriptsRender() => _memoryCache.GetOrCreate($"{Id}_{PlaceHead}", entry =>
    {
        entry.SlidingExpiration = TimeSpan.FromMinutes(30);
        return HeadScriptsRender0();
    })!;

    public string HeadScriptsRender0()
    {
        return _providers.Where(s => s.Place == PlaceHead)
                            .OrderBy(s => s.Order)
                            .Select(s => s.Provider.HtmlContent())
                            .JoinStr("\n\t");
    }

    public string FooterScriptsRender() => _memoryCache.GetOrCreate($"{Id}_{PlaceFooter}", entry =>
    {
        entry.SlidingExpiration = TimeSpan.FromMinutes(30);
        return FooterScriptsRender0();
    })!;

    public string FooterScriptsRender0()
    {
        return _providers.Where(s => s.Place == PlaceFooter)
                            .OrderBy(s => s.Order)
                            .Select(s => s.Provider.HtmlContent())
                            .JoinStr("\n\t");
    }

    public void ClearCache()
    {
        _memoryCache.Remove($"{Id}_{PlaceHead}");
        _memoryCache.Remove($"{Id}_{PlaceFooter}");
    }
}

internal record SiteAssetItem(string Key, ISiteAssetPrivider Provider, float Order, string Place);
