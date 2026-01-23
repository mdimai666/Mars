using System.Collections.Immutable;
using Mars.Host.Shared.Utils;
using Mars.Shared.Contracts.WebSite.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.Template;

namespace Mars.Host.Shared.WebSite.Models;

public class WebPage : WebSitePart
{
    public PathString Url { get; init; } //maybe multiple urls?

    /// <summary>
    /// Имеются ли параметры типа /path/<b>{id}</b>
    /// </summary>
    public bool UrlIsContainCurlyBracket { get; private init; }
    public int UrlSegmentCount;

    /// <summary>
    /// Имеются ли фильтры у параметров типа /path/{id<b>:int</b>}
    /// </summary>
    public bool IsRoutePatternHasConstraints { get; private init; }
    public RouteTemplate RouteTemplate { get; private init; }
    public RoutePattern RoutePattern { get; private init; }
    public TemplateMatcher TemplateMatcher { get; private init; }

    private RouteValueDictionary? _templateMatcherRouteValues;
    private IReadOnlyDictionary<string, IRouteConstraint>? _routeConstraints;

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

        TemplateMatcher = new TemplateMatcher(RouteTemplate, _templateMatcherRouteValues ??= []);
        IsRoutePatternHasConstraints = TemplateMatcherUsedConstraints().Length > 0;

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

        if (TemplateMatcher is null) return false;
        routeValues = CompiledHttpRouteMatcher.RouteValuePools.Get();

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
            CompiledHttpRouteMatcher.RouteValuePools.Return(routeValues);
            throw;
        }
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

    public static WebPage Blank(string html, string? title = null, string url = "/")
    {
        return new WebPage(new WebSitePart(WebSitePartType.Page, "unsetpage", "unsetpage.html", "unsetpage.html", html, new Dictionary<string, string>() { ["page"] = url }, title));
    }
}
