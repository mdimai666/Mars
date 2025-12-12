using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;

namespace Mars.Host.Shared.Utils;

public static class RouteUtil
{
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
    /// Constraints filter builder
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
}
