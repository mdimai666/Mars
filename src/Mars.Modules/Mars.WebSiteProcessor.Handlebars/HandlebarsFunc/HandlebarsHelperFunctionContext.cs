using Mars.Host.Shared.WebSite.Models;
using Microsoft.AspNetCore.Http.Features;

namespace Mars.Host.Templators.HandlebarsFunc;

public class HandlebarsHelperFunctionContext : IDisposable
{
    public const string HelperFunctionContextKey = "rctx";

    public PageRenderContext PageContext { get; private set; }
    public IServiceProvider ServiceProvider { get; private set; }//TODO: after complete remove this, context function use as DI
    public CancellationToken CancellationToken { get; }
    public IFeatureCollection Features { get; set; } = new FeatureCollection();

    public HandlebarsHelperFunctionContext(PageRenderContext pageContext, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        PageContext = pageContext;
        ServiceProvider = serviceProvider;
        CancellationToken = cancellationToken;
    }

    public void Dispose()
    {
        // Без принудительного =null шаблонизатор не отпускает объекты.
        PageContext = null!;
        ServiceProvider = null!;
        Features = null!;
    }
}
