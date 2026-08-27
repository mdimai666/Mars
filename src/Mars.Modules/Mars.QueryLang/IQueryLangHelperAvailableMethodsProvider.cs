using Mars.SiteEngine.Abstractions.Templators;
using Mars.Nodes.Core.Models.EntityQuery;

namespace Mars.QueryLang;

public interface IQueryLangHelperAvailableMethodsProvider
{
    IReadOnlyCollection<TemplatorHelperInfoAttribute> AvailableMethods();
    IReadOnlyCollection<LinqMethodSignarute> LinqMethodSignarutes();
}
