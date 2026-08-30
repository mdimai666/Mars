using Mars.SiteEngine.Abstractions.Templators;

namespace Mars.QueryLang.Services;

public interface IQueryLangLinqDatabaseQueryHandler
{
    public Task<object?> Handle(string linqExpression, XInterpreter ppt, CancellationToken cancellationToken);
}
