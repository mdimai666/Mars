using System.Reflection;
using Mars.QueryLang.Contracts;
using Mars.QueryLang.Host.Services;
using Mars.SiteEngine.Abstractions.Templators;

namespace Mars.QueryLang.Host.Helpers;

internal class QueryLangHelperAvailableMethodsProvider : IQueryLangHelperAvailableMethodsProvider
{
    private IReadOnlyCollection<TemplatorHelperInfoAttribute>? _items;
    private IReadOnlyCollection<LinqMethodSignature>? _signatures;

    public IReadOnlyCollection<TemplatorHelperInfoAttribute> AvailableMethods()
    {
        if (_items != null) return _items;

        var mock = new EfStringQuery(null!, null!);
        var methods = mock.MethodsMapping();

        return _items = methods.Select(x => x.Value.GetCustomAttribute<TemplatorHelperInfoAttribute>()).Where(x => x != null).ToList()!;
    }

    public IReadOnlyCollection<LinqMethodSignature> LinqMethodSignatures()
    {
        if (_signatures != null) return _signatures;

        var b = new EfStringQuery(null!, null!);
        var methods = b.MethodsMapping();

        var items = AvailableMethods();
        var dict = items.Select(s => new MethodHelperInfo(s.Shortcut, s.Example, s.Description)).GroupBy(x => x.Shortcut).ToDictionary(s => s.Key, s => s.First());

        Func<string, LinqMethodParameter[], LinqMethodSignature> ff = (name, param) => new(name, param, dict[name]!);

        List<LinqMethodSignature> signatures = [
            ff(nameof(b.Count),[ new() ]),
            ff(nameof(b.Any),[ new() ]),
            ff(nameof(b.All),[ new() ]),
            ff(nameof(b.First),[ new() ]),
            ff(nameof(b.Last),[ new() ]),
            ff(nameof(b.Skip),[ new() ]),
            ff(nameof(b.Take),[ new() ]),
            ff(nameof(b.Where),[ new() ]),
            ff(nameof(b.Select),[ new() ]),
            ff(nameof(b.OrderBy),[ new() ]),
            ff(nameof(b.OrderByDescending),[ new() ]),
            ff(nameof(b.ThenBy),[ new() ]),
            ff(nameof(b.ThenByDescending),[ new() ]),
            ff(nameof(b.ToList),[ new() ]),
            ff(nameof(b.Distinct),[ new() ]),
            ff(nameof(b.DistinctBy),[ new() ]),
            ff(nameof(b.Max),[ new() ]),
            ff(nameof(b.Min),[ new() ]),
            ff(nameof(b.MaxBy),[ new() ]),
            ff(nameof(b.MinBy),[ new() ]),
            ff(nameof(b.Sum),[ new() ]),
            ff(nameof(b.Average),[ new() ]),
            ff(nameof(b.GroupBy),[ new() ]),

            ff(nameof(b.Include),[ new() ]),
            ff(nameof(b.Union),[ new() ]),

            ff(nameof(b.Search),[ new() ]),
            ff(nameof(b.Table),[ new("@page"), new("@pageSize") ]),
        ];

        return _signatures = signatures;
    }
}
