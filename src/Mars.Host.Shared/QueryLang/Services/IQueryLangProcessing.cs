using Mars.Host.Shared.WebSite.Models;

namespace Mars.Host.Shared.QueryLang.Services;

public interface IQueryLangProcessing
{
    Task<Dictionary<string, object?>> Process(
        PageRenderContext pageContext,
        IReadOnlyCollection<KeyValuePair<string, string>> Queries,
        Dictionary<string, object>? localVaribles,
        CancellationToken cancellationToken);
}
