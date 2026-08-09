using System.Text;
using Mars.Core.Models;
using Mars.Host.Shared.Hubs;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Templators;
using Mars.Host.Shared.WebSite.Interfaces;
using Mars.Host.Shared.WebSite.Models;
using Mars.Host.Templators.HandlebarsFunc;
using Mars.Shared.Contracts.WebSite.Models;
using Mars.WebSiteProcessor.Handlebars.TemplateData;
using Mars.WebSiteProcessor.Interfaces;
using Mars.WebSiteProcessor.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebSiteProcessor.Handlebars;

public class HandlebarsWebRenderEngine : IWebRenderEngine
{
    protected MarsAppFront AppFront = default!;
    private IMemoryCache? _memoryCache;
    private IMarsHtmlTemplator? _marsHtmlTemplator;

    public HandlebarsWebRenderEngine(IMemoryCache? memoryCache, MarsAppFront marsAppFront)
    {
        AppFront = marsAppFront;
        _memoryCache = memoryCache;
    }

    public virtual void Setup()
    {
        if (string.IsNullOrEmpty(AppFront.Configuration.Path))
        {
            throw new ArgumentNullException("cfg: AppFront.Path");
        }
    }

    /// <summary>
    /// Инициализация движка вне пайплайна (создание через IWebRenderEngineFactory)
    /// </summary>
    public void InitializeEngine(IServiceProvider rootServices)
    {
        Initialize(AppFront, rootServices);
    }

    protected virtual void Initialize(MarsAppFront appFront, IServiceProvider rootServices)
    {
        var hub = rootServices.GetRequiredService<IHubContext<ChatHub>>();
        var wts = new WebTemplateService(rootServices, hub, appFront);
        appFront.Features.Set<IWebTemplateService>(wts);

        wts.OnFileUpdated += (s, e) =>
        {
            wts.ClearCache();
        };

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

        IMarsHtmlTemplator.MarsHtmlTemplate<object, object>? template_compiled;

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

            IMarsHtmlTemplator handlebars = _marsHtmlTemplator ??= new MyHandlebars();
            handlebars.RegisterContextFunctions();

            if (parts is not null)
            {
                foreach (var block in parts.Where(s => s.Type == WebSitePartType.Block || s.Type == WebSitePartType.Layout))
                {
                    handlebars.RegisterTemplate(block.Name, block.Content);
                }
            }
            template_compiled = handlebars.Compile(combined_html.ToString());
            _memoryCache?.Set(AppCacheKey(af, page, ctx.RenderParam), template_compiled, DateTimeOffset.Now.AddMinutes(30));

#if DEBUG2
            stopwatch.Stop();
            Console.WriteLine($"render_finish: {stopwatch.ElapsedMilliseconds}ms. Page:{page?.Url}");
            var z2 = GC.GetTotalMemory(false);
#endif
        }

        var tmpFillers = (ITemplateContextVariblesFiller[])[
            new HandlebarsTmpCtxBasicDataContext(),
            new HandlebarsTmpCtxLanguageDataFiller(),
            new HandlebarsTmpCtxAppThemeFiller(),
            new HandlebarsTmpCtxErrorsListFiller(),
        ];

        foreach (var filler in tmpFillers)
        {
            filler.FillTemplateDictionary(ctx, ctx.TemplateContextVaribles);
        }

        _ = nameof(HandlebarsHelperFunctionContext.HelperFunctionContextKey);

        // Без принудительного =null шаблонизатор не отпускает объекты.
        using var hctx = new HandlebarsHelperFunctionContext(ctx, serviceProvider, cancellationToken);

        var result = template_compiled(ctx.TemplateContextVaribles, new { rctx = hctx } /*это необходимо для зарегестированных функций*/);

        return result;
    }
}
