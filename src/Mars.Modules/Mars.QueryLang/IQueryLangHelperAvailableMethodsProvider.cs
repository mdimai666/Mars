using Mars.Nodes.Core.Models.EntityQuery;
using Mars.SiteEngine.Abstractions.Templators;

namespace Mars.QueryLang;

public interface IQueryLangHelperAvailableMethodsProvider
{
    IReadOnlyCollection<TemplatorHelperInfoAttribute> AvailableMethods();
    IReadOnlyCollection<LinqMethodSignarute> LinqMethodSignarutes();
}
