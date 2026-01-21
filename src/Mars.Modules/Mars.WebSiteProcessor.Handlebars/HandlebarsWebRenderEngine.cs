using System.Diagnostics;
using System.Text;
using Mars.Core.Models;
using Mars.Host.Shared.Hubs;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Templators;
using Mars.Host.Shared.WebSite;
using Mars.Host.Shared.WebSite.Interfaces;
using Mars.Host.Shared.WebSite.Models;
using Mars.Host.Templators.HandlebarsFunc;
using Mars.Shared.Contracts.WebSite.Models;
using Mars.WebSiteProcessor.Handlebars.TemplateData;
using Mars.WebSiteProcessor.Interfaces;
using Mars.WebSiteProcessor.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Mars.WebSiteProcessor.Handlebars;

public class HandlebarsWebRenderEngine : IWebRenderEngine
{
    protected MarsAppFront AppFront = default!;
    private IMemoryCache? _memoryCache;
    //private readonly IMarsHtmlTemplator _marsHtmlTemplator;

    public HandlebarsWebRenderEngine()
    {
        //_memoryCache = memoryCache;
    }

    public virtual void AddFront(WebApplicationBuilder builder, MarsAppFront appFront)
    {
        AppFront = appFront;

        if (AppFront.Configuration.Mode == Mars.Core.Models.AppFrontMode.HandlebarsTemplateStatic && string.IsNullOrEmpty(AppFront.Configuration.Path))
        {
            throw new ArgumentNullException("cfg: AppFront.Path");
        }
    }

    public virtual void UseFront(IApplicationBuilder app)
    {
        Initialize(AppFront, app.ApplicationServices);
        _memoryCache = app.ApplicationServices.GetRequiredService<IMemoryCache>();

        var front = app;

        if (AppFront.Configuration.Mode == AppFrontMode.HandlebarsTemplateStatic)
        {
            bool hasWwwRoot = Directory.Exists(Path.Join(AppFront.Configuration.Path, "wwwroot"));
            if (hasWwwRoot)
            {
                string path = Path.Join(AppFront.Configuration.Path, "wwwroot");
                front.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(path),
                    //RequestPath = appFront.Configuration.Url,
                    ServeUnknownFileTypes = true,
                });
            }
        }

        IWebSiteProcessor webSiteProcessor = app.ApplicationServices.GetRequiredService<IWebSiteProcessor>();

        if (AppFront.Configuration.Mode != AppFrontMode.None)
        {
            front.UseEndpoints(endpoints =>
            {
                endpoints.MapFallback(webSiteProcessor.Response);
            });
        }
        else
        {
            front.UseEndpoints(endpoints =>
            {
                endpoints.MapFallback(async req =>
                {
                    req.Response.StatusCode = StatusCodes.Status404NotFound;
                    await req.Response.WriteAsync("None");
                }).ShortCircuit();
            });
        }

    }

    protected virtual void Initialize(MarsAppFront appFront, IServiceProvider rootServices)
    {
        var hub = rootServices.GetRequiredService<IHubContext<ChatHub>>();
        var wts = new WebTemplateService(rootServices, hub, appFront);
        appFront.Features.Set<IWebTemplateService>(wts);

        wts.OnFileUpdated += (s, e) =>
        {
            //wts.ScanSite();
            wts.ClearCache();
        };

    }

    string GetRenderKey(MarsAppFront appFront, RenderParam renderParam, WebPage page, WebSiteTemplate template)
    {
        string key = $"{appFront.Configuration.Url}::{template.Hash}::{page.FileRelPath}_{(renderParam.AllowLayout ? "1" : "0")}-{(renderParam.OnlyBody ? "1" : "0")}";
        return key;
    }

    public virtual string RenderPage(RenderEngineRenderRequestContext renderContext, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var template = renderContext.WebSiteTemplate;
        return RenderPage(renderContext.AppFront, renderContext.PageContext, template.RootPage, renderContext.Page, template.Parts, serviceProvider, cancellationToken);
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

        //IHandlebars handlebars = MyHandlebars.GetMyHandlebars();
        //IMemoryCache memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();

        //var af = renderContext.HttpContext.Items[nameof(MarsAppFront)] as MarsAppFront;
        //string cacheKey = GetRenderKey(af, renderContext.RenderParam, page, renderContext.WebSiteTemplate);

        IMarsHtmlTemplator.MarsHtmlTemplate<object, object> template_compiled = null!;

        //if (_memoryCache?.TryGetValue(cacheKey, out template_compiled!) ?? false)
        //{

        //}
        //else
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

#if DEBUG
            var z1 = GC.GetTotalMemory(false);
            Stopwatch stopwatch = Stopwatch.StartNew();
#endif

            using IMarsHtmlTemplator handlebars = new MyHandlebars();
            handlebars.RegisterContextFunctions();

            if (parts is not null)
            {
                foreach (var block in parts.Where(s => s.Type == WebSitePartType.Block || s.Type == WebSitePartType.Layout))
                {
                    handlebars.RegisterTemplate(block.Name, block.Content);
                }
            }
            template_compiled = handlebars.Compile(combined_html.ToString());
            //_memoryCache?.Set(cacheKey, template_compiled, DateTimeOffset.Now.AddMinutes(10));

#if DEBUG
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

        var hctx = new HandlebarsHelperFunctionContext(ctx, serviceProvider, cancellationToken);

        var result = template_compiled(ctx.TemplateContextVaribles, new { rctx = hctx } /*это необходимо для зарегестированных функций*/);

        //var z3 = GC.GetTotalMemory(false);

        //var a0 = (z1 - z0).ToHumanizedSize();
        //var a1 = (z2 - z1).ToHumanizedSize();
        //var a2 = (z3 - z2).ToHumanizedSize();

        //if (renderContext.RenderParam.LegacyBody)
        //{
        //    result = result.Split("</head>", 2)[1].Split("</body>", 2)[0];
        //    result = result.Split('>', 2)[1];
        //}

        //if (renderContext.RenderParam.OnlyBody)
        //{

        //    if (result.Contains("<endmaui>"))
        //    {
        //        result = result.Split("<endmaui>", 2)[0] + "</body></html>";

        //    }
        //}

        return result;
    }
}
