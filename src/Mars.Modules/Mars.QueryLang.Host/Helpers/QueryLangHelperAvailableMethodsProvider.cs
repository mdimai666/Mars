using System.Reflection;
using Mars.Host.Data.Common;
using Mars.Host.Shared.Templators;
using Mars.QueryLang.Host.Services;

namespace Mars.QueryLang.Host.Helpers;

internal class QueryLangHelperAvailableMethodsProvider : IQueryLangHelperAvailableMethodsProvider
{
    static IReadOnlyCollection<TemplatorHelperInfoAttribute>? _items;

    public IReadOnlyCollection<TemplatorHelperInfoAttribute> AvailableMethods()
    {
        if (_items != null) return _items;

        var mock = new EfStringQuery<IBasicEntity>(null!, null!);
        var methods = mock.MethodsMapping();

        return _items = methods.Select(x => x.Value.GetCustomAttribute<TemplatorHelperInfoAttribute>()).Where(x => x != null).ToList()!;
    }
}
