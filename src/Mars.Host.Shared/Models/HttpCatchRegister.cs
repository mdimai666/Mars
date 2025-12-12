using System.Collections.Immutable;
using Mars.Host.Shared.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.Template;

namespace Mars.Host.Shared.Models;

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

    public HttpCatchRegister(string method, string pattern, string nodeId)
    {
        Method = method;
        Pattern = pattern;
        NodeId = nodeId;

        IsContainCurlyBracket = pattern.Contains('{');

        RouteTemplate = TemplateParser.Parse(Pattern);
        RoutePattern = RouteTemplate.ToRoutePattern();

        //TemplateMatcher = new TemplateMatcher(RouteTemplate, GetDefaults(RouteTemplate));
        TemplateMatcher = new TemplateMatcher(RouteTemplate, _templateMatcherRouteValues ??= []);
        IsRoutePatternHasConstraints = TemplateMatcherUsedConstraints().Length > 0;
    }

    public bool TryMatch(PathString path, out RouteValueDictionary? routeValues)
    {
        if (TemplateMatcher is null)
        {
            routeValues = null;
            return false;
        }

        routeValues = new RouteValueDictionary();
        var match = TemplateMatcher.TryMatch(path, routeValues);

        //Если совподает шаблон, дополнительно проверить типы {id:int:min(5)}. и т.д.
        if (IsRoutePatternHasConstraints)
        {
            match = RouteConstraintMatch(path, routeValues);
        }

        return match;
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
