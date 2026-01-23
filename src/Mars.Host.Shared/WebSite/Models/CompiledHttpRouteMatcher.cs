using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.ObjectPool;

namespace Mars.Host.Shared.WebSite.Models;

public class CompiledHttpRouteMatcher
{
    public static readonly ObjectPool<RouteValueDictionary> RouteValuePools =
        ObjectPool.Create(new RouteValueDictionaryPolicy());

    // Для точных путей — словарь (O(1))
    private Dictionary<string, WebPage> _exactRoutes { get; }

    // Key is segments count
    private Dictionary<int, List<WebPage>> _templateRoutes { get; }

    private bool _empty;

    private WebPage IndexPage { get; }

    public CompiledHttpRouteMatcher(IReadOnlyCollection<WebPage> pages, WebPage indexPage)
    {
        var exact = new Dictionary<string, WebPage>();
        var templates = new Dictionary<int, List<WebPage>>();

        foreach (var reg in pages)
        {
            if (!reg.UrlIsContainCurlyBracket)
            {
                // Точные пути — нормализуем к нижнему регистру
                var key = reg.Url.Value.ToLower().TrimEnd('/');
                exact[key] = reg;
            }
            else
            {
                if (!templates.TryGetValue(reg.UrlSegmentCount, out var list))
                {
                    list = [];
                    templates[reg.UrlSegmentCount] = list;
                }
                templates[reg.UrlSegmentCount].Add(reg);
            }
        }

        _exactRoutes = exact;
        _templateRoutes = templates;

        _empty = _exactRoutes.Count + _templateRoutes.Count == 0;
        IndexPage = indexPage;

    }

    public WebPage? Match(PathString pathString, out RouteValueDictionary? routeValues)
    {
        routeValues = null;
        if (_empty)
        {
            return null;
        }

        if (pathString == "/") return IndexPage;

        if (_exactRoutes.TryGetValue(pathString.Value.ToLower().TrimEnd('/'), out var exactMatch))
        {
            return exactMatch;
        }

        if (_templateRoutes.Count > 0)
        {
            var segments = pathString.Value!.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (_templateRoutes.TryGetValue(segments.Length, out var templates))
            {
                foreach (var template in templates)
                {
                    if (template.MatchUrl(pathString, out routeValues))
                    {
                        return template;
                    }
                }
            }
        }
        return null;
    }
}

file class RouteValueDictionaryPolicy : IPooledObjectPolicy<RouteValueDictionary>
{
    public RouteValueDictionary Create() => [];

    public bool Return(RouteValueDictionary obj)
    {
        obj.Clear(); // обязательно очищать!
        return true;
    }
}
