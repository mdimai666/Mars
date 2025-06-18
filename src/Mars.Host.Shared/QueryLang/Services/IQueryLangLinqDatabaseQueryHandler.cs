using Mars.Host.Shared.Templators;

namespace Mars.Host.Shared.QueryLang.Services;

public interface IQueryLangLinqDatabaseQueryHandler
{
    public Task<object?> Handle(string linqExpression, XInterpreter ppt, CancellationToken cancellationToken);
}
