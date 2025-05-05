using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Logging;

namespace Mars.Host.Shared.Models;

public class HttpCatchRegister
{
    public Guid Id { get; set; } = Guid.NewGuid();

    string _method = "GET";
    public string Method { get => _method; set => _method = value.ToUpper(); }
    public string Pattern { get; set; } = default!;
    public string NodeId { get; set; } = default!;

    //public string isStaticUrl

    public bool IsContainCurlyBracket;
    public RouteTemplate RouteTemplate;
    public RoutePattern RoutePattern;

    public TemplateMatcher TemplateMatcher { get; }

    public HttpCatchRegister(string method, string pattern, string nodeId)
    {
        Method = method;
        Pattern = pattern;
        NodeId = nodeId;

        IsContainCurlyBracket = pattern.Contains('{');

        RouteTemplate = TemplateParser.Parse(Pattern);
        RoutePattern = RouteTemplate.ToRoutePattern();

        //this.TemplateMatcher = new Microsoft.AspNetCore.Routing.Template.TemplateMatcher(this.RouteTemplate, new RouteValueDictionary());
        TemplateMatcher = new TemplateMatcher(RouteTemplate, GetDefaults(RouteTemplate));
        //var b = x2.TryMatch(context.Request.Path, new RouteValueDictionary());
    }

    private RouteValueDictionary GetDefaults(RouteTemplate parsedTemplate)
    {
        var result = new RouteValueDictionary();

        foreach (var parameter in parsedTemplate.Parameters)
        {
            if (parameter.DefaultValue != null)
            {
                result.Add(parameter.Name!, parameter.DefaultValue);
            }
        }

        return result;
    }

    public bool TryMatch(PathString path, HttpContext httpContext, ILogger logger)
    {
        //PathString p = new PathString(a.Pattern);
        //var x = new RoutePatternFactory.
        //var x = Microsoft.AspNetCore.Routing.Template.RouteTemplate
        //var x2 = new Microsoft.AspNetCore.Routing.Template.TemplateMatcher()
        //var x = Microsoft.AspNetCore.Routing.Template.TemplateParser.Parse(a.Pattern);

        //Microsoft.AspNetCore.Routing.Template.TemplateMatcher
        //Microsoft.AspNetCore.Routing.Constraints.defa

        //var match = TemplateMatcher.TryMatch(path, TemplateMatcher.Defaults);
        var match = TemplateMatcher.TryMatch(path, TemplateMatcher.Defaults);
        //var match = RouteConstraintMatcher.Match(GetDefaultConstraintMap() as IDictionary<string, IRouteConstraint>, new RouteValueDictionary(),
        //    httpContext, new , RouteDirection.IncomingRequest, logger);

        //RouteTemplate.
        //TemplateMatcher.
        //RoutePattern.

        return match;
    }


    private static IDictionary<string, Type> GetDefaultConstraintMap()
    {
        return new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
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
        };
    }


}
