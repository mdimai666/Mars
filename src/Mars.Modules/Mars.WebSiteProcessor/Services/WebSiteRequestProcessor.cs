using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite.Exceptions;
using Mars.Host.Shared.WebSite.Models;
using Mars.Options.Models;
using Mars.WebSiteProcessor.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using RenderInfo = Mars.Host.Shared.WebSite.Models.RenderInfo;

namespace Mars.WebSiteProcessor.Services;

public class WebSiteRequestProcessor
{
    protected readonly IServiceProvider serviceProvider;
    protected readonly WebSiteTemplate template;
    protected readonly IOptionService optionService;
    protected readonly IRequestContext requestContext;

    public WebSiteRequestProcessor(IServiceProvider serviceProvider, WebSiteTemplate template)
    {
        this.serviceProvider = serviceProvider;
        this.template = template;
        optionService = serviceProvider.GetRequiredService<IOptionService>();
        requestContext = serviceProvider.GetRequiredService<IRequestContext>();
    }

    public async Task<RenderInfo> RenderRequest(HttpContext httpContext, RenderParam param, CancellationToken cancellationToken)
    {
        var request = new WebClientRequest(httpContext.Request);
        var appFront = (httpContext.Items[nameof(MarsAppFront)] as MarsAppFront)!;
        var maintenanceModeOption = optionService.GetOption<MaintenanceModeOption>();

        if (maintenanceModeOption.Enable)
        {
            return await RenderMaintenancePage(appFront, request, maintenanceModeOption, param, cancellationToken);
        }

        WebPage? page = ResolveUrl(httpContext.Request.Path);

        if (page is null) return await RenderPage404(appFront, request, param, cancellationToken);

        return new RenderInfo
        {
            Code = 200,
            html = await RenderPageHtml(appFront, request, page, param, cancellationToken),
            Title = page.Title ?? page.Name,
        };

    }

    public async Task<RenderInfo> RenderPage(MarsAppFront appFront, WebClientRequest request, WebPage page, RenderParam param, CancellationToken cancellationToken)
    {
        return new RenderInfo
        {
            Code = 200,
            html = await RenderPageHtml(appFront, request, page, param, cancellationToken),
            Title = page.Title ?? page.Name,
        };
    }

    public async Task<RenderInfo> RenderPage404(MarsAppFront appFront, WebClientRequest request, RenderParam param, CancellationToken cancellationToken)
    {
        return new RenderInfo
        {
            Code = 404,
            html = await RenderPageHtml(appFront, request, template.Page404 ?? template.IndexPage, param, cancellationToken),
            Title = "404"
        };
    }

    public async Task<RenderInfo> RenderMaintenancePage(MarsAppFront appFront, WebClientRequest request, MaintenanceModeOption option, RenderParam param, CancellationToken cancellationToken)
    {
        WebPage webPage;
        string pageTitle;
        //param.OnlyBody = true; Check it

        if (option.MaintenancePageSource == EMaintenancePageSource.StaticHtml)
        {
            webPage = WebPage.Blank(option.MaintenanceStaticPageContent, option.MaintenanceStaticPageTitle);
            pageTitle = option.MaintenanceStaticPageTitle;
        }
        else if (option.MaintenancePageSource == EMaintenancePageSource.PostPage)
        {
            webPage = template.Pages.FirstOrDefault(post => post.Url == option.RenderPageUrl) ?? WebPage.Blank("Maintenance Page not found", "503");
            pageTitle = webPage.Title ?? webPage.Name;
        }
        else
        {
            throw new NotImplementedException();
        }

        return new RenderInfo
        {
            Code = 503,
            html = await RenderPageHtml(appFront, request, webPage, param, cancellationToken),
            Title = pageTitle
        };
    }

    public WebPage? ResolveUrl(PathString path)
    {
        string uri = path.ToString().TrimEnd('/');
        if (string.IsNullOrEmpty(uri)) uri = "/";

        WebPage? page = template.Pages.FirstOrDefault(s => string.Equals(uri, s.Url, StringComparison.OrdinalIgnoreCase));

        if (page is null)
        {
            page = template.Pages
                    .Where(post => post.UrlIsContainCurlyBracket)
            .FirstOrDefault(page => page.MatchUrl(path));
        }

        return page;
    }

    /// <summary>
    /// RenderPageHtml
    /// </summary>
    /// <param name="appFront"></param>
    /// <param name="request"></param>
    /// <param name="page"></param>
    /// <param name="param"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="RenderPageHtmlException"></exception>
    async Task<string> RenderPageHtml(MarsAppFront appFront, WebClientRequest request, WebPage page, RenderParam param, CancellationToken cancellationToken)
    {
        //RenderEngineRenderRequestContext renderContext = null!;

        try
        {
            bool isCache = false;
            string cacheKey = GetPageCacheKey(page, request);
            IMemoryCache? memoryCache = null;
            string? cachevalue = null;

            bool hasQuery = request.Query.Any();

            if (!hasQuery && page.Attributes.TryGetValue("cache-force", out cachevalue))
            {
                isCache = true;
                memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();

                if (memoryCache.TryGetValue(cacheKey, out string? html))
                {
                    return html!;
                }
            }
            else if (!optionService.IsDevelopment && !hasQuery && page.Attributes.TryGetValue("cache", out cachevalue))
            {
                isCache = true;
                memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();

                if (memoryCache.TryGetValue(cacheKey, out string? html))
                {
                    return html!;
                }
            }

            var userService = serviceProvider.GetRequiredService<IUserService>();

            //=======================================================
            //PREPARE context

            UserDetail? userDetail = null;
            if (requestContext.IsAuthenticated)
            {
                userDetail = await userService.GetDetail(requestContext.User.Id, cancellationToken);
            }

            //var hostHtml = new PrepareHostHtml2();
            var pageRenderContext = new PageRenderContext()
            {
                Request = request,
                SysOptions = optionService.SysOption,
                User = userDetail == null ? null : new RenderContextUser(userDetail),
                RenderParam = param,
                IsDevelopment = optionService.IsDevelopment,
            };

            var renderContext = new RenderEngineRenderRequestContext(request, appFront, template, page, pageRenderContext, param);

            //PageRenderContextOld ctx = await PrepareHostHtml.PreparePageContext(httpContext, serviceProvider, requestContext, cancellationToken);

            pageRenderContext.TemplateContextVaribles.Add("$attr", page.Attributes);

            if (page.UrlIsContainCurlyBracket)
            {
                //Dictionary<string, object> par = new();

                var surl = TemplateParser.Parse(request.Path);

                for (int i = 0; i < page.RoutePattern.PathSegments.Count; i++)
                {
                    var p = page.RoutePattern.PathSegments[i].Parts[0];

                    if (p.IsParameter && p is RoutePatternParameterPart pa && i < surl.Segments.Count)
                    {
                        var seg = surl.Segments[i].Parts[0].Text;
                        var key = pa.Name;
                        //httpContext.Request.RouteValues.Add(key, seg); //TODO: rgis - slug conflict
                        pageRenderContext.TemplateContextVaribles.TryAdd(key, seg!);
                    }
                }

                //var pars = httpContext.Request.RouteValues;

                #region EXPERIMENTS
                ////var z = page.TemplateMatcher.Template.Parameters;
                ////var e = httpContext.Request.RouteValues;
                ////var d = httpContext.GetRouteData();
                ////var d2 = httpContext.GetRouteValue("ID");

                //var template = TemplateParser.Parse(page.Url);
                //var matcher = new Microsoft.AspNetCore.Routing.Template.TemplateMatcher(template, httpContext.Request.RouteValues);
                ////var values = matcher.TryMatch(httpContext.Request.Path);

                ////RouteConstraintMatcher.Match(
                ////    WebPage.GetDefaultConstraintMap() as IDictionary<string, IRouteConstraint>,
                ////    httpContext.Request.RouteValues,
                ////    httpContext,
                ////    null,
                ////    RouteDirection.IncomingRequest,
                ////    null
                ////);
                ///

                //var template = TemplateParser.Parse(page.Url);
                //var routePattern = template.ToRoutePattern();
                //var matcher = new Microsoft.AspNetCore.Routing.Template.TemplateMatcher(template, httpContext.Request.RouteValues);
                //var pp = matcher.Template.Parameters;

                //var f = httpContext.Features;

                //var q = httpContext.Request.Query;
                //var v = httpContext.Request.RouteValues;
                //var node = new UrlMatchingNode(0);
                //node.
                //var matcher = new TemplateMatcher(entry.RouteTemplate, entry.Defaults);
                #endregion

            }

            //=======================================================
            //PREPARE handlebars
            //WebClientRequest req = new WebClientRequest(httpContext.Request);

            //var af = httpContext.Items[nameof(MarsAppFront)] as MarsAppFront;

            IWebRenderEngine renderEngine = appFront.Features.Get<IWebRenderEngine>()!;
            ArgumentNullException.ThrowIfNull(renderEngine, nameof(renderEngine));

            //renderContext = new(httpContext, template, page, ctx, param);

            if(request.Items.TryGetValue(typeof(RenderPageHtmlException), out var ex))
            {
                renderContext.PageContext.TemplateContextVaribles.Add("ex", ex);
            }

            var result = renderEngine.RenderPage(renderContext, serviceProvider, cancellationToken);

            if (isCache)
            {
                var tsCache = ParseStringTimespan(cachevalue ?? "10m");
                memoryCache?.Set(cacheKey, result, tsCache ?? TimeSpan.FromMinutes(5));
            }

            return result;
        }
        catch (Exception ex)
        {
            throw new RenderPageHtmlException(
                page,
                [],
                ex.Message,
                ex);
        }
    }

    string GetPageCacheKey(WebPage page, WebClientRequest request) => $"{page.Name}+{request.Path}";

    TimeSpan? ParseStringTimespan(string simespanString)
    {
        string[] formats = { @"m\m", @"h\h\m\m", @"s\s" };
        TimeSpan ts;
        if (TimeSpan.TryParseExact(simespanString, formats, null, out ts))
        {
            return ts;
        }
        else
        {
            return null;
        }
    }

}

///////////// Things....

#if THINGS
public class TemplatePipline
{
    public void Processing()
    {
        /*
        - PrepareContext
        - Compile
        - Render
        - AfterFilter
        */
    }
}

public interface IWebSiteRenderPipline//ProcessingPipline
{
    public PageRenderContext CreateContext(HttpContext httpContext);
    public WebPage FindPage();
    public IWebRenderEngine RenderEngine { get; }
}

public interface IContentRender
{
    public void Render(HttpContext httpContext, string content);
}

public class StaticWebsiteProcessing : IWebSiteRenderPipline
{
    public IWebRenderEngine RenderEngine { get; }

    public PageRenderContext CreateContext(HttpContext httpContext)
    {
        throw new NotImplementedException();
    }

    public WebPage FindPage()
    {
        throw new NotImplementedException();
    }
}
#endif
