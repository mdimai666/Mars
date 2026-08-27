using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.ObjectPool;

namespace Mars.Nodes.Host.Shared.HttpModule;

public class CompiledHttpRouteMatcher
{
    public static readonly ObjectPool<RouteValueDictionary> RouteValuePools =
        ObjectPool.Create(new RouteValueDictionaryPolicy());

    // Для точных путей — словарь (O(1))
    private Dictionary<string, HttpCatchRegister> _exactRoutes { get; }

    // Для шаблонов — список, но уже отсортированный один раз!
    // segments count
    private Dictionary<int, List<HttpCatchRegister>> _templateRoutes { get; }

    private bool _empty;

    public CompiledHttpRouteMatcher(IReadOnlyCollection<HttpCatchRegister> httpCatches)
    {
        var exact = new Dictionary<string, HttpCatchRegister>();
        var templates = new Dictionary<int, List<HttpCatchRegister>>();

        foreach (var reg in httpCatches)
        {
            if (!reg.IsContainCurlyBracket)
            {
                // Точные пути — нормализуем к нижнему регистру
                var key = reg.Pattern.ToLower().TrimEnd('/');
                exact[key] = reg;
            }
            else
            {
                if (!templates.TryGetValue(reg.SegmentCount, out var list))
                {
                    list = [];
                    templates[reg.SegmentCount] = list;
                }
                templates[reg.SegmentCount].Add(reg);
            }
        }

        _exactRoutes = exact;
        _templateRoutes = templates;

        _empty = _exactRoutes.Count + _templateRoutes.Count == 0;

        //return new CompiledHttpRouteMatcher
        //{
        //    ExactRoutes = exact,
        //    TemplateRoutes = templates // можно сортировать один раз, если нужно
        //};
    }

    public HttpCatchRegister? Match(PathString pathString, out RouteValueDictionary? routeValues)
    {
        routeValues = null;
        if (_empty)
        {
            return null;
        }

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
                    if (template.TryMatchFast(pathString, out routeValues))
                    {
                        return template;
                    }
                }
            }
        }
        return null;
    }
}

// Где-то в DI или статически (лучше через DI)
//public static class RouteValuePools
//{
//    public static readonly ObjectPool<RouteValueDictionary> Pool =
//        ObjectPool.Create(new RouteValueDictionaryPolicy());
//}

file class RouteValueDictionaryPolicy : IPooledObjectPolicy<RouteValueDictionary>
{
    public RouteValueDictionary Create() => [];

    public bool Return(RouteValueDictionary obj)
    {
        obj.Clear(); // обязательно очищать!
        return true;
    }
}
