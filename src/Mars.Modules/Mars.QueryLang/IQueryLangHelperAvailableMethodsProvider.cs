using Mars.Host.Shared.Templators;

namespace Mars.QueryLang;

public interface IQueryLangHelperAvailableMethodsProvider
{
    IReadOnlyCollection<TemplatorHelperInfoAttribute> AvailableMethods();
}
