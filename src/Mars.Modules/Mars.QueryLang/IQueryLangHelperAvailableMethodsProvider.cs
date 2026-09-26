using Mars.QueryLang.Contracts;
using Mars.SiteEngine.Abstractions.Templators;

namespace Mars.QueryLang;

public interface IQueryLangHelperAvailableMethodsProvider
{
    IReadOnlyCollection<TemplatorHelperInfoAttribute> AvailableMethods();
    IReadOnlyCollection<LinqMethodSignature> LinqMethodSignatures();
}
