using System.Text;
using HandlebarsDotNet;
using Mars.SiteEngine.Abstractions.Models;
using Mars.SiteEngine.Abstractions.TemplateData;
using Mars.SiteEngine.Abstractions.WebSite;
using Mars.SiteEngine.Abstractions.WebSite.Models;
using Mars.SiteEngine.Contracts.WebSite.Models;
using Mars.SiteEngine.Handlebars.HandlebarsFunc;
using Mars.TemplateEngine.Providers.HandlebarsProvider;
using Microsoft.Extensions.Caching.Memory;

namespace Mars.SiteEngine.Handlebars;

public class HandlebarsWebRenderEngine : IWebRenderEngine
{
    protected MarsAppFront AppFront = default!;
    private readonly IMemoryCache? _memoryCache;
    private readonly IHandlebars _handlebars;

    public HandlebarsWebRenderEngine(IMemoryCache? memoryCache, IHandlebarsEngineFactory handlebarsEngineFactory, MarsAppFront marsAppFront)
    {
        AppFront = marsAppFront;
        _memoryCache = memoryCache;
        _handlebars = handlebarsEngineFactory.Create(HandlebarsScopes.Site);
    }

    public virtual void Setup()
    {
        if (string.IsNullOrEmpty(AppFront.Configuration.Path))
        {
            throw new ArgumentNullException("cfg: AppFront.Path");
        }

        if (!Directory.Exists(AppFront.Configuration.Path))
        {
            throw new DirectoryNotFoundException($"Front folder not found '{AppFront.Configuration.Path}'");
        }
    }

    public virtual string RenderPage(RenderEngineRenderRequestContext renderContext, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var template = renderContext.WebSiteTemplate;
        return RenderPage(renderContext.AppFront, renderContext.PageContext, template.RootPage, renderContext.Page, template.Parts, serviceProvider, cancellationToken);
    }

    string AppCacheKey(MarsAppFront appFront, WebPage? page, RenderParam renderParam)
    {
        return $"HandlebarsWebRenderEngine::{appFront.Configuration.Url}::AppCacheKey[{page?.Url},{(renderParam.OnlyBody ? 1 : 0)},{(renderParam.AllowLayout ? 1 : 0)}]";
    }

    public virtual string RenderPage(
        MarsAppFront MarsAppFront,
        PageRenderContext ctx,
        WebRoot root,
        WebPage? page,
        IReadOnlyCollection<WebSitePart>? parts,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var af = MarsAppFront;

        HandlebarsTemplate<object, object>? template_compiled;

        if (ctx.RenderParam.UseCache && _memoryCache?.TryGetValue(AppCacheKey(af, page, ctx.RenderParam), out template_compiled) == true)
        {

        }
        else
        {

            StringBuilder combined_html = new();
            string? beforeHtml = null;
            string? afterHtml = null;
            var onlyBody = ctx.RenderParam.OnlyBody;

            if (!onlyBody)
            {
                var prep = new RootPageBodyTagSplitter(root.Content);
                beforeHtml = prep.PreBody;
                afterHtml = prep.AfterBody;

                combined_html.AppendLine(beforeHtml);
            }

            string? layoutBlockName = null;

            if ((ctx.RenderParam.AllowLayout || !ctx.RenderParam.OnlyBody)
                && (page?.Layout is not null || root.DefaultLayout is not null))
            {
                layoutBlockName = page.Layout ?? root.DefaultLayout;
            }

            if (layoutBlockName is not null)
                combined_html.AppendLine($"{{{{#>{layoutBlockName}}}}}");

            if (page is not null)
            {
                combined_html.AppendLine(page.Content);
            }

            if (layoutBlockName is not null)
                combined_html.AppendLine($"{{{{/{layoutBlockName}}}}}");

            if (!onlyBody)
            {
                combined_html.AppendLine(afterHtml);
            }

#if DEBUG2
            var z1 = GC.GetTotalMemory(false);
            Stopwatch stopwatch = Stopwatch.StartNew();
#endif

            if (parts is not null)
            {
                foreach (var block in parts.Where(s => s.Type == WebSitePartType.Block || s.Type == WebSitePartType.Layout))
                {
                    _handlebars.RegisterTemplate(block.Name, block.Content);
                }
            }
            template_compiled = _handlebars.Compile(combined_html.ToString());
            _memoryCache?.Set(AppCacheKey(af, page, ctx.RenderParam), template_compiled, DateTimeOffset.Now.AddMinutes(30));

#if DEBUG2
            stopwatch.Stop();
            Console.WriteLine($"render_finish: {stopwatch.ElapsedMilliseconds}ms. Page:{page?.Url}");
            var z2 = GC.GetTotalMemory(false);
#endif
        }

        var tmpFillers = (ITemplateContextVariablesFiller[])[
            new SiteTmpCtxBasicDataContext(),
            new SiteTmpCtxLanguageDataFiller(),
            new SiteTmpCtxAppThemeFiller(),
            new SiteTmpCtxErrorsListFiller(),
        ];

        foreach (var filler in tmpFillers)
        {
            filler.FillTemplateDictionary(ctx, ctx.TemplateContextVariables);
        }

        ctx.TemplateContextVariables[SiteTmpCtxBasicDataContext.SiteBaseParamKey] = SiteBaseHref.FromFrontUrl(af.Front?.Url);

        _ = nameof(HandlebarsHelperFunctionContext.HelperFunctionContextKey);

        // Без принудительного =null шаблонизатор не отпускает объекты.
        using var hctx = new HandlebarsHelperFunctionContext(ctx, serviceProvider, cancellationToken);

        var result = template_compiled(ctx.TemplateContextVariables, new { rctx = hctx } /*это необходимо для зарегестированных функций*/);

        return result;
    }
}
