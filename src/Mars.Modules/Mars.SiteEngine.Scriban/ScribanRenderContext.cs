using Mars.SiteEngine.Abstractions.WebSite.Models;
using Scriban;
using Scriban.Runtime;

namespace Mars.SiteEngine.Scriban;

/// <summary>
/// Контекст одного рендера, доступный сайт-функциям Scriban через <see cref="TemplateContext.Tags"/>.
/// </summary>
public class ScribanRenderContext
{
    public static readonly object TagKey = typeof(ScribanRenderContext);

    public PageRenderContext PageContext { get; }
    public IServiceProvider ServiceProvider { get; }
    public CancellationToken CancellationToken { get; }
    public WebSiteTemplate? WebSiteTemplate { get; }

    /// <summary>
    /// Глобальные сайт-функции движка (для справочника {{ help }}).
    /// </summary>
    public ScriptObject Functions { get; }

    /// <summary>
    /// Данные рендера (переменные шаблона) — сайт-функции дописывают результаты запросов сюда,
    /// чтобы они были видны последующим блокам и второй стадии (layout).
    /// </summary>
    public ScriptObject DataObject { get; }

    public ScribanRenderContext(
        PageRenderContext pageContext,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken,
        WebSiteTemplate? webSiteTemplate,
        ScriptObject dataObject,
        ScriptObject functions)
    {
        PageContext = pageContext;
        ServiceProvider = serviceProvider;
        CancellationToken = cancellationToken;
        WebSiteTemplate = webSiteTemplate;
        DataObject = dataObject;
        Functions = functions;
    }

    public static ScribanRenderContext From(TemplateContext context)
    {
        if (context.Tags.TryGetValue(TagKey, out var rctx) && rctx is ScribanRenderContext renderContext)
        {
            return renderContext;
        }

        throw new InvalidOperationException("ScribanRenderContext not found in TemplateContext.Tags — функция вызвана вне сайт-движка Scriban");
    }
}
