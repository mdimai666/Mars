using System.Collections.Immutable;
using Mars.Shared.Contracts.WebSite.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
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

        {
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

            //try
            //{
            RouteTemplate = TemplateParser.Parse(Url.Value);
            RoutePattern = RouteTemplate.ToRoutePattern();

            //this.TemplateMatcher = new Microsoft.AspNetCore.Routing.Template.TemplateMatcher(this.RouteTemplate, new RouteValueDictionary());
            TemplateMatcher = new TemplateMatcher(RouteTemplate, _templateMatcherRouteValues ??= new());
            IsRoutePatternHasConstraints = TemplateMatcherUsedConstraints().Length > 0;
            //}
            //catch (Exception ex)
            //{
            //    //Log
            //    //ILogger<WebPage> a;
            //    //a.LogError()
            //    //LoggerFactory.
            //    //Console.Out.err
            //    //Console.Error.WriteLine($"page Url not valid: file:{FileFullPath} url:{Url} \n {ex.Message} ");

            //    MarsLogger.GetStaticLogger<WebPage>().LogError(ex, $"page Url not valid: file:{FileFullPath} url:{Url} \n {ex.Message}");

            //}

        }

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

    public static readonly IReadOnlyDictionary<string, Type> DefaultConstraintMap =
        new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            // Type-specific constraints
            { "int", typeof(IntRouteConstraint) },
            { "bool", typeof(BoolRouteConstraint) },
            { "datetime", typeof(DateTimeRouteConstraint) },
            { "decimal", typeof(DecimalRouteConstraint) },
            { "double", typeof(DoubleRouteConstraint) },
            { "float", typeof(FloatRouteConstraint) },
            { "guid", typeof(GuidRouteConstraint) },
            { "long", typeof(LongRouteConstraint) },

            // Length constraints
            { "minlength", typeof(MinLengthRouteConstraint) },
            { "maxlength", typeof(MaxLengthRouteConstraint) },
            { "length", typeof(LengthRouteConstraint) },

            // Min/Max value constraints
            { "min", typeof(MinRouteConstraint) },
            { "max", typeof(MaxRouteConstraint) },
            { "range", typeof(RangeRouteConstraint) },

            // Regex-based constraints
            { "alpha", typeof(AlphaRouteConstraint) },
            { "regex", typeof(RegexInlineRouteConstraint) },

            { "required", typeof(RequiredRouteConstraint) },

            { "file", typeof(FileNameRouteConstraint) },
            { "nonfile", typeof(NonFileNameRouteConstraint) },
        };

    /// <summary>
    /// </summary>
    /// <param name="constrainFuncs">
    /// <list type="bullet">
    /// <item>int</item>
    /// <item>minlength(10)</item>
    /// </list>
    /// </param>
    /// <returns></returns>
    public static IReadOnlyDictionary<string, IRouteConstraint> CreateConstraints(IEnumerable<string> constrainFuncs)
      => new Dictionary<string, IRouteConstraint>(constrainFuncs.Select(constrainFunc =>
      {
          var sp = constrainFunc.Split('(', 2);
          var constrainName = sp[0];
          string? argString = sp.Length == 1 ? null : sp[1][..^1];
          string[]? args = argString?.Split(',', StringSplitOptions.TrimEntries);
          var type = DefaultConstraintMap[constrainName];
          int[]? _ints = args?.Select(s => int.TryParse(s, out var _int) ? _int : -1).ToArray();
          int _int = _ints?[0] ?? -1;

          var instance = constrainName switch
          {
              "minlength" => new MinLengthRouteConstraint(_int),
              "maxlength" => new MaxLengthRouteConstraint(_int),
              "length" => new LengthRouteConstraint(_int),
              "min" => new MinRouteConstraint(_int),
              "max" => new MaxRouteConstraint(_int),
              "range" => new RangeRouteConstraint(_ints[0], _ints[1]),

              "regex" => new RegexInlineRouteConstraint(argString!),

              _ => Activator.CreateInstance(type) as IRouteConstraint
          };

          return new KeyValuePair<string, IRouteConstraint>(constrainFunc, instance!);
      }));

    public bool MatchUrl(PathString path)
    {
        if (TemplateMatcher is null) return false;

        var routeValues = new RouteValueDictionary();
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
            _routeConstraints = CreateConstraints(constrainUsed).ToImmutableDictionary();
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
