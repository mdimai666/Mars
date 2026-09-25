using System.Collections.Immutable;
using Mars.Server.Abstractions.Utils;
using Mars.SiteEngine.Contracts.WebSite.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.Template;

namespace Mars.SiteEngine.Abstractions.WebSite.Models;

public class WebPage : WebSitePart
{
    public PathString Url { get; init; } //maybe multiple urls?

    /// <summary>
    /// Имеются ли параметры типа /path/<b>{id}</b>
    /// </summary>
    public bool UrlIsContainCurlyBracket { get; private init; }
    public int UrlSegmentCount { get; private init; }

    /// <summary>
    /// Имеются ли фильтры у параметров типа /path/{id<b>:int</b>}
    /// </summary>
    public bool IsRoutePatternHasConstraints { get; private init; }

    RouteTemplate RouteTemplate { get; init; }
    RoutePattern RoutePattern { get; init; }
    TemplateMatcher TemplateMatcher { get; init; }

    readonly string[] _usedConstraints;
    IReadOnlyDictionary<string, IRouteConstraint>? _routeConstraints;

    public string? Layout { get; init; }
    public bool DefineLayout { get; init; }

    //roles, layout

    public WebPage(WebSitePart part, string? url = null, string? title = null) : base(part)
    {
        Type = WebSitePartType.Page;

        if (string.IsNullOrEmpty(url))
        {
            var pageAttribute = Attributes["page"];
            Url = pageAttribute == "/" ? "/" : pageAttribute.TrimEnd('/');
        }
        else
        {
            Url = url;
        }

        UrlIsContainCurlyBracket = Url.Value.Contains('{');

        RouteTemplate = TemplateParser.Parse(Url.Value);
        RoutePattern = RouteTemplate.ToRoutePattern();
        UrlSegmentCount = RouteTemplate.Segments.Count;

        TemplateMatcher = new TemplateMatcher(RouteTemplate, new RouteValueDictionary());
        _usedConstraints = TemplateMatcherUsedConstraints();
        IsRoutePatternHasConstraints = _usedConstraints.Length > 0;

        if (Attributes.TryGetValue("layout", out var layoutName))
        {
            DefineLayout = true;
            if (layoutName != "null")
            {
                Layout = layoutName;
            }
        }

        Title ??= title ?? Attributes.GetValueOrDefault("title") ?? Name;
    }

    public bool MatchUrl(PathString path, out RouteValueDictionary? routeValues)
    {
        routeValues = null;
        if (path.Value.EndsWith('/'))
            path = path.Value[..^1];
        if (path.Value.Split('/', StringSplitOptions.RemoveEmptyEntries).Length != UrlSegmentCount)
        {
            return false;
        }

        if (!UrlIsContainCurlyBracket)
            return path == Url;

        routeValues = WebPageRouteMatcher.RouteValuePools.Get();

        try
        {
            var match = TemplateMatcher.TryMatch(path, routeValues!);

            //Если совподает шаблон, дополнительно проверить типы {id:int:min(5)}. и т.д.
            if (match && IsRoutePatternHasConstraints)
            {
                match = RouteConstraintMatch(path, routeValues!);
            }

            return match;
        }
        catch
        {
            // Если что-то пошло не так — возвращаем в пул
            WebPageRouteMatcher.RouteValuePools.Return(routeValues);
            throw;
        }
    }

    /// <summary>
    /// Раскладывает сегменты url по параметрам маршрута ({slug} и т.п.) в переменные шаблона.
    /// </summary>
    public void FillRouteVariables(PathString path, Dictionary<string, object?> templateContextVariables)
    {
        if (!UrlIsContainCurlyBracket) return;

        var surl = TemplateParser.Parse(path);

        for (int i = 0; i < RoutePattern.PathSegments.Count && i < surl.Segments.Count; i++)
        {
            var p = RoutePattern.PathSegments[i].Parts[0];

            if (p.IsParameter && p is RoutePatternParameterPart pa)
            {
                var seg = surl.Segments[i].Parts[0].Text;
                templateContextVariables.TryAdd(pa.Name, seg!);
            }
        }
    }

    string[] TemplateMatcherUsedConstraints()
        => TemplateMatcher.Template.Parameters.SelectMany(s => s.InlineConstraints.Select(s => s.Constraint)).Distinct().ToArray();

    public bool RouteConstraintMatch(PathString pathString, RouteValueDictionary routeValues)
    {
        _routeConstraints ??= RouteUtil.CreateConstraints(_usedConstraints).ToImmutableDictionary();

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

    public static WebPage Blank(string html, string? title = null, string url = "/")
    {
        return new WebPage(new WebSitePart(WebSitePartType.Page, "unsetpage", "unsetpage.html", "unsetpage.html", html, new Dictionary<string, string>() { ["page"] = url }, title));
    }
}
