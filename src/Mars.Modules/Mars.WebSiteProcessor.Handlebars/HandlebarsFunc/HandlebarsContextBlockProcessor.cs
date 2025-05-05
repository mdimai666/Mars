using Mars.Host.Shared.QueryLang.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Host.Templators.HandlebarsFunc;

public class HandlebarsContextBlockProcessor
{
    public async Task Process(HandlebarsHelperFunctionContext ctx)
    {
        var queryLangProcessor = ctx.ServiceProvider.GetRequiredService<IQueryLangProcessing>();
        var pageRenderContext = ctx.PageContext;

        foreach (var q in pageRenderContext.DataQueries.Values)
        {
            var result = await queryLangProcessor.Process(pageRenderContext, q.Queries, null, ctx.CancellationToken);
            q.Complete = true;
            q.ResultDict = result;

            foreach (var (key, val) in result)
            {
                pageRenderContext.TemplateContextVaribles.TryAdd(key, val);
            }
        }
    }
}
