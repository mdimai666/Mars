using Mars.Host.Shared.WebSite.Models;

namespace Mars.Host.Shared.QueryLang.Services;

public interface IQueryLangLinqDatabaseQueryHandler
{
    public Task<object?> Handle(string linqExpression, PageRenderContext pageContext, Dictionary<string, object>? localVaribles, CancellationToken cancellationToken);
}
