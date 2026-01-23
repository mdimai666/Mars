using System.Collections.Immutable;
using Mars.Host.Shared.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.Template;

namespace Mars.Nodes.Host.Shared.HttpModule;

public class HttpCatchRegister
{
    public Guid Id { get; set; } = Guid.NewGuid();

    string _method = "GET";
    public string Method { get => _method; set => _method = value.ToUpper(); }
    public string Pattern { get; set; } = default!;
    public string NodeId { get; set; } = default!;

    /// <summary>
    /// Имеются ли параметры типа /path/<b>{id}</b>
    /// </summary>
    public bool IsContainCurlyBracket { get; private init; }

    /// <summary>
    /// Имеются ли фильтры у параметров типа /path/{id<b>:int</b>}
    /// </summary>
    public bool IsRoutePatternHasConstraints { get; private init; }
    protected RouteTemplate RouteTemplate;
    protected RoutePattern RoutePattern;
    protected TemplateMatcher TemplateMatcher;
    private RouteValueDictionary? _templateMatcherRouteValues;
    private IReadOnlyDictionary<string, IRouteConstraint>? _routeConstraints;

    private readonly PathSegment[] _segments;
    public int SegmentCount;

    public HttpCatchRegister(string method, string pattern, string nodeId)
    {
        Method = method;
        Pattern = pattern;
        NodeId = nodeId;

        IsContainCurlyBracket = pattern.Contains('{');

        RouteTemplate = TemplateParser.Parse(Pattern);
        RoutePattern = RouteTemplate.ToRoutePattern();
        SegmentCount = RouteTemplate.Segments.Count;

        //segments for fast matching
        var parts = pattern.Split('/', StringSplitOptions.RemoveEmptyEntries);
        _segments = new PathSegment[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            _segments[i] = new PathSegment(parts[i]);
        }

        //TemplateMatcher = new TemplateMatcher(RouteTemplate, GetDefaults(RouteTemplate));
        TemplateMatcher = new TemplateMatcher(RouteTemplate, _templateMatcherRouteValues ??= []);
        IsRoutePatternHasConstraints = TemplateMatcherUsedConstraints().Length > 0;
    }

#if OLD
    public bool TryMatch(PathString requestPath, out RouteValueDictionary? routeValues)
    {
        if (TemplateMatcher is null)
        {
            routeValues = null;
            return false;
        }

        routeValues = [];
        var match = TemplateMatcher.TryMatch(requestPath, routeValues);

        //Если совподает шаблон, дополнительно проверить типы {id:int:min(5)}. и т.д.
        if (match && IsRoutePatternHasConstraints)
        {
            match = RouteConstraintMatch(requestPath, routeValues);
        }

        return match;
    } 
#endif

    public bool TryMatchFast(PathString requestPath, out RouteValueDictionary? routeValues)
    {
        routeValues = null;

        ReadOnlySpan<char> path = requestPath.Value.AsSpan();

        if (path.IsEmpty || path[0] != '/')
            return false;

        // Убираем начальный '/'
        if (path.Length == 1)
            path = ReadOnlySpan<char>.Empty;
        else
            path = path[1..];

        // Подсчитываем количество сегментов в запросе
        int expectedSegments = SegmentCount;
        int actualSegments = CountSegments(path);

        if (actualSegments != expectedSegments)
            return false;

        // Выделяем словарь только если совпадение возможно
        var dict = CompiledHttpRouteMatcher.RouteValuePools.Get();

        try
        {
            int segIndex = 0;
            int start = 0;

            for (int i = 0; i <= path.Length; i++)
            {
                if (i == path.Length || path[i] == '/')
                {
                    ReadOnlySpan<char> segment = path.Slice(start, i - start);
                    ref readonly var templateSeg = ref _segments[segIndex];

                    if (templateSeg.IsParameter)
                    {
                        // Извлекаем значение параметра
                        string value = segment.ToString(); // ← единственная аллокация (неизбежна)

                        // Если есть ограничения — проверяем позже (или сейчас, если простые)
                        //routeValues[templateSeg.ParameterName!] = value;
                        dict[templateSeg.ParameterName!] = value;
                    }
                    else
                    {
                        // Точный сегмент: сравниваем без аллокаций
                        if (!segment.Equals(templateSeg.Raw.AsSpan(), StringComparison.OrdinalIgnoreCase))
                            return false;
                    }

                    segIndex++;
                    start = i + 1;
                }
            }

            routeValues = dict;

            // Дополнительно: проверка ограничений (если есть)
            if (IsRoutePatternHasConstraints)
            {
                return RouteConstraintMatch(requestPath, routeValues);
            }
            return true;
        }
        catch
        {
            // Если что-то пошло не так — возвращаем в пул
            CompiledHttpRouteMatcher.RouteValuePools.Return(dict);
            throw;
        }
    }

    private static int CountSegments(ReadOnlySpan<char> path)
    {
        if (path.IsEmpty) return 0;
        int count = 1;
        for (int i = 0; i < path.Length; i++)
        {
            if (path[i] == '/') count++;
        }
        return count;
    }

    public string[] TemplateMatcherUsedConstraints()
        => TemplateMatcher.Template.Parameters.SelectMany(s => s.InlineConstraints.Select(s => s.Constraint)).Distinct().ToArray();

    public bool RouteConstraintMatch(PathString pathString, RouteValueDictionary routeValues)
    {
        if (_routeConstraints is null)
        {
            var constrainUsed = TemplateMatcherUsedConstraints();
            _routeConstraints = RouteUtil.CreateConstraints(constrainUsed).ToImmutableDictionary();
        }

        //var constraints = CreateConstraints(["id:int"]);
        foreach (var (key, val) in routeValues)
        {
            var par = TemplateMatcher.Template.GetParameter(key);
            foreach (var inlineCon in par.InlineConstraints)
            {
                var constraint = _routeConstraints[inlineCon.Constraint];
                var match = constraint.Match(null, null, key, routeValues, RouteDirection.IncomingRequest);
                if (!match) return false;
            }

        }
        return true;
    }

}
