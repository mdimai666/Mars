using Mars.SiteEngine.Abstractions.WebSite.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.QueryLang.Services;

/// <summary>
/// Исполняет накопленные в <see cref="PageRenderContext.DataQueries"/> запросы
/// и добавляет результаты в TemplateContextVariables. Общий для всех движков сайта.
/// </summary>
public static class ContextQueryProcessor
{
    public static async Task Process(PageRenderContext pageRenderContext, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var queryLangProcessor = serviceProvider.GetRequiredService<IQueryLangProcessing>();

        foreach (var q in pageRenderContext.DataQueries.Values)
        {
            var result = await queryLangProcessor.Process(pageRenderContext, q.Queries, null, cancellationToken);
            q.Complete = true;
            q.ResultDict = result;

            foreach (var (key, val) in result)
            {
                pageRenderContext.TemplateContextVariables.TryAdd(key, val);
            }
        }
    }
}
