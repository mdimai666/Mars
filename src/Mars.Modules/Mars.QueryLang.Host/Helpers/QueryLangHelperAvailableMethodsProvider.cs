using System.Reflection;
using Mars.Host.Data.Common;
using Mars.Host.Shared.Templators;
using Mars.Nodes.Core.Models.EntityQuery;
using Mars.QueryLang.Host.Services;

namespace Mars.QueryLang.Host.Helpers;

internal class QueryLangHelperAvailableMethodsProvider : IQueryLangHelperAvailableMethodsProvider
{
    private IReadOnlyCollection<TemplatorHelperInfoAttribute>? _items;
    private IReadOnlyCollection<LinqMethodSignarute>? _signarutes;

    public IReadOnlyCollection<TemplatorHelperInfoAttribute> AvailableMethods()
    {
        if (_items != null) return _items;

        var mock = new EfStringQuery<IBasicEntity>(null!, null!);
        var methods = mock.MethodsMapping();

        return _items = methods.Select(x => x.Value.GetCustomAttribute<TemplatorHelperInfoAttribute>()).Where(x => x != null).ToList()!;
    }

    public IReadOnlyCollection<LinqMethodSignarute> LinqMethodSignarutes()
    {
        if (_signarutes != null) return _signarutes;

        var b = new EfStringQuery<IBasicEntity>(null!, null!);
        var methods = b.MethodsMapping();

        var items = AvailableMethods();
        var dict = items.Select(s => new MethodHelperInfo(s.Shortcut, s.Example, s.Description)).GroupBy(x => x.Shortcut).ToDictionary(s => s.Key, s => s.First());

        Func<string, LinqMethodParameter[], LinqMethodSignarute> ff = (name, param) => new(name, param, dict[name]!);

        List<LinqMethodSignarute> signarutes = [
            ff(nameof(b.Count),[ new() ]),
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

            ff(nameof(b.Include),[ new() ]),
            ff(nameof(b.Union),[ new() ]),

            ff(nameof(b.Search),[ new() ]),
            ff(nameof(b.Table),[ new("@page"), new("@pageSize") ]),
        ];

        return _signarutes = signarutes;
    }
}
