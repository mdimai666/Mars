using System.Text;
using Mars.SiteEngine.Abstractions.Models;
using Mars.SiteEngine.Abstractions.TemplateData;
using Mars.SiteEngine.Abstractions.WebSite;
using Mars.SiteEngine.Abstractions.WebSite.Models;
using Mars.SiteEngine.Contracts.WebSite.Models;
using Mars.TemplateEngine.Providers.ScribanProvider;
using Microsoft.Extensions.Caching.Memory;
using Scriban;
using Scriban.Runtime;

namespace Mars.SiteEngine.Scriban;

/// <summary>
/// Сайт-движок на Scriban: двухстадийный рендер (страница → layout-обёртка с переменной body),
/// include-блоки/лейауты через <see cref="WebSitePartsTemplateLoader"/>.
/// Конвенция layout-шаблонов Scriban: содержимое страницы выводится через {{ body }}.
/// </summary>
public class ScribanWebRenderEngine : IWebRenderEngine
{
    public const string BodyVariable = "body";

    protected MarsAppFront AppFront = default!;
    private readonly IMemoryCache? _memoryCache;
    private readonly ScriptObject _globalFunctions;

    public ScribanWebRenderEngine(IMemoryCache? memoryCache, IScribanEngineFactory scribanEngineFactory, MarsAppFront marsAppFront)
    {
        AppFront = marsAppFront;
        _memoryCache = memoryCache;
        _globalFunctions = scribanEngineFactory.CreateGlobalObject(ScribanScopes.Site);
    }

    record CompiledSet(Template Wrapper, Template Page);

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
        return RenderPage(renderContext.AppFront, renderContext.PageContext, template.RootPage, renderContext.Page, template.Parts, template, serviceProvider, cancellationToken);
    }

    string AppCacheKey(MarsAppFront appFront, WebPage? page, RenderParam renderParam)
    {
        return $"ScribanWebRenderEngine::{appFront.Configuration.Url}::AppCacheKey[{page?.Url},{(renderParam.OnlyBody ? 1 : 0)},{(renderParam.AllowLayout ? 1 : 0)}]";
    }

    public virtual string RenderPage(
        MarsAppFront appFront,
        PageRenderContext ctx,
        WebRoot root,
        WebPage? page,
        IReadOnlyCollection<WebSitePart>? parts,
        WebSiteTemplate? webSiteTemplate,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        CompiledSet? compiled;

        if (ctx.RenderParam.UseCache && _memoryCache?.TryGetValue(AppCacheKey(appFront, page, ctx.RenderParam), out compiled) == true)
        {

        }
        else
        {
            compiled = CompilePageSet(ctx, root, page, parts);
            _memoryCache?.Set(AppCacheKey(appFront, page, ctx.RenderParam), compiled, DateTimeOffset.Now.AddMinutes(30));
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

        var dataObject = new ScriptObject();
        foreach (var (key, val) in ctx.TemplateContextVariables)
        {
            dataObject[key] = val!;

            // в Scriban `$name` — локальная переменная, глобальные так не прочитать:
            // сайт-переменные $errors/$maui/... дублируются алиасом без префикса
            if (key.Length > 1 && key[0] == '$')
            {
                dataObject[key[1..]] = val!;
            }
        }
        dataObject["mobile"] = ctx.Request.IsMobile;

        var rctx = new ScribanRenderContext(ctx, serviceProvider, cancellationToken, webSiteTemplate, dataObject);

        var templateContext = new TemplateContext
        {
            // свойства CLR-объектов доступны как есть (_user.FullName), без snake_case-переименования
            MemberRenamer = member => member.Name,
            TemplateLoader = new WebSitePartsTemplateLoader(parts),
            CancellationToken = cancellationToken,
        };
        templateContext.Tags[ScribanRenderContext.TagKey] = rctx;
        templateContext.PushGlobal(_globalFunctions);
        templateContext.PushGlobal(dataObject);

        var bodyHtml = compiled.Page.Render(templateContext);
        dataObject[BodyVariable] = bodyHtml;

        return compiled.Wrapper.Render(templateContext);
    }

    CompiledSet CompilePageSet(PageRenderContext ctx, WebRoot root, WebPage? page, IReadOnlyCollection<WebSitePart>? parts)
    {
        var onlyBody = ctx.RenderParam.OnlyBody;

        string? beforeHtml = null;
        string? afterHtml = null;
        if (!onlyBody)
        {
            var prep = new RootPageBodyTagSplitter(root.Content);
            beforeHtml = prep.PreBody;
            afterHtml = prep.AfterBody;
        }

        string? layoutName = null;
        if ((ctx.RenderParam.AllowLayout || !ctx.RenderParam.OnlyBody)
            && (page?.Layout is not null || root.DefaultLayout is not null))
        {
            layoutName = page?.Layout ?? root.DefaultLayout;
        }

        string? layoutSource = null;
        if (layoutName is not null)
        {
            layoutSource = parts?.FirstOrDefault(p =>
                p.Name == layoutName
                && (p.Type == WebSitePartType.Block || p.Type == WebSitePartType.Layout))?.Content;

            if (layoutSource is null)
            {
                throw new InvalidOperationException($"ScribanWebRenderEngine: layout '{layoutName}' not found in front parts (Block/Layout)");
            }
        }

        var wrapperText = new StringBuilder();
        if (!onlyBody) wrapperText.Append(beforeHtml);
        wrapperText.Append(layoutSource ?? $"{{{{ {BodyVariable} }}}}");
        if (!onlyBody) wrapperText.Append(afterHtml);

        var wrapper = ParseTemplate(wrapperText.ToString(), "_wrapper");
        var pageTemplate = ParseTemplate(page?.Content ?? "", page?.Name ?? "_page");

        return new CompiledSet(wrapper, pageTemplate);
    }

    static Template ParseTemplate(string text, string sourceName)
    {
        var template = Template.Parse(text, sourceName);
        if (template.HasErrors)
        {
            throw new InvalidOperationException($"ScribanWebRenderEngine: template '{sourceName}' parse errors: {string.Join("; ", template.Messages)}");
        }

        return template;
    }
}
