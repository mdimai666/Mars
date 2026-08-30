using Mars.SiteEngine.Abstractions.WebSite.Models;

namespace Mars.QueryLang.Services;

public interface IQueryLangProcessing
{
    Task<Dictionary<string, object?>> Process(
        PageRenderContext pageContext,
        IReadOnlyCollection<KeyValuePair<string, string>> Queries,
        Dictionary<string, object>? localVariables,
        CancellationToken cancellationToken);
}
