using Mars.Host.Shared.Templators;
using Mars.Nodes.Core.Models.EntityQuery;

namespace Mars.QueryLang;

public interface IQueryLangHelperAvailableMethodsProvider
{
    IReadOnlyCollection<TemplatorHelperInfoAttribute> AvailableMethods();
    IReadOnlyCollection<LinqMethodSignarute> LinqMethodSignarutes();
}
