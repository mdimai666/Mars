using System.Web;
using Mars.Core.Models;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Dto.Renders;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite.Interfaces;
using Mars.Host.Shared.WebSite.Models;
using Mars.Shared.Common;
using Mars.Shared.Contracts.WebSite.Models;
using Mars.WebSiteProcessor.Interfaces;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;

namespace Mars.Services;

internal class PageRenderService : IPageRenderService
{
    public PageRenderService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    string[] disallowRenderTypes = { "template", "block", "layout" };
    private readonly IServiceProvider _serviceProvider;

    void ModifyRequest(HttpContext httpContext)
    {
        //httpContext.Request.Path = httpContext.Request.Path.ToString().Split("/api/Post/Render/", 2)[1];

        if (httpContext.Request.Path.StartsWithSegments("/api") == false) return;

        string replLink = httpContext.Request.Path.ToString().Contains("/api/PageRender/RenderUrl/")
            ? "/api/PageRender/RenderUrl/"
            : "/api/PageRender/Render/";

        int su_count = replLink.Length;
        var su_path = "/" + HttpUtility.UrlDecode(httpContext.Request.Path.ToString().Substring(su_count));
        if (su_path.StartsWith("//")) su_path = su_path.Substring(1, su_path.Length - 1);
        httpContext.Request.Path = su_path;

        var _uri = new Uri(httpContext.Request.GetDisplayUrl());
        httpContext.Request.Path = _uri.AbsolutePath;

        httpContext.Request.QueryString = new QueryString(_uri.Query);
        var qq = System.Web.HttpUtility.ParseQueryString(_uri.Query);

        var q_dict = new Dictionary<string, StringValues>();
        foreach (var key in qq.AllKeys)
        {
            q_dict.Add(key, qq[key]);
        }
        httpContext.Request.Query = new QueryCollection(q_dict);
    }

    RenderParam RenderParam(HttpContext context) => new()
    {
        //OnlyBody = ParseQueryBool(context, "OnlyBody"),
        OnlyBody = true,
        AllowLayout = ParseQueryBool(context, "Layout")
    };

    bool ParseQueryBool(HttpContext context, string name)
    {
        return context.Request.Query.TryGetValue(name, out var val)
            ? (bool.TryParse(val, out var result) ? result : false)
            : false;
    }

    public async Task<RenderActionResult<PostRenderDto>> RenderPage(WebPage page, HttpContext httpContext, CancellationToken cancellationToken)
    {
        ModifyRequest(httpContext);

        var af = httpContext.Items[nameof(MarsAppFront)] as MarsAppFront;
        var request = new WebClientRequest(httpContext.Request);

        var tsv = af.Features.Get<IWebTemplateService>();
        WebSiteProcessor.Services.WebSiteRequestProcessor processor = new(_serviceProvider, tsv.Template);
        var render = await processor.RenderPage(af, request, page, RenderParam(httpContext), cancellationToken);

        return AsResult(render, httpContext);
    }

    public async Task<RenderActionResult<PostRenderDto>> RenderPage404(HttpContext httpContext, CancellationToken cancellationToken)
    {
        ModifyRequest(httpContext);

        var af = httpContext.Items[nameof(MarsAppFront)] as MarsAppFront;
        var request = new WebClientRequest(httpContext.Request);

        var tsv = af.Features.Get<IWebTemplateService>();
        WebSiteProcessor.Services.WebSiteRequestProcessor processor = new(_serviceProvider, tsv.Template);
        var render = await processor.RenderPage404(af, request, RenderParam(httpContext), cancellationToken);

        return AsResult(render, httpContext);
    }

    RenderActionResult<PostRenderDto> AsResult(Host.Shared.WebSite.Models.RenderInfo render, HttpContext httpContext)
    {
        var _uri = new Uri(httpContext.Request.GetDisplayUrl());

        return new RenderActionResult<PostRenderDto>
        {
            Ok = true,
            NotFound = render.Code == 404,
            Message = "OK",
            Data = new PostRenderDto
            {
                Html = render.html,
                PostSlug = _uri.LocalPath,
                Title = render.Title,
            }
        };
    }

    RenderActionResult<PostRenderDto> RenderNotSupportError()
        => new()
        {
            Ok = false,
            Message = "AppFront mode not support render"
        };

    bool IsRenderNotSupport(MarsAppFront af)
    {
        var renderEngine = af.Features.Get<IWebRenderEngine>();
        return renderEngine is null;
    }

    public async Task<RenderActionResult<PostRenderDto>> RenderUrl(PathString url, HttpContext httpContext, RenderParam? renderParam = null, CancellationToken cancellationToken = default)
    {
        //WebClientRequest req = new(httpContext.Request);
        //if (httpContext.Request.HasFormContentType)
        //{
        //    var form = httpContext.Request.Form;
        //}

        var af = httpContext.Items[nameof(MarsAppFront)] as MarsAppFront;
        ArgumentNullException.ThrowIfNull(af, nameof(af));

        if (IsRenderNotSupport(af)) return RenderNotSupportError();
        ModifyRequest(httpContext);

        var tsv = af.Features.Get<IWebTemplateService>();
        WebSiteProcessor.Services.WebSiteRequestProcessor processor = new(_serviceProvider, tsv.Template);

        Host.Shared.WebSite.Models.RenderInfo render = await processor.RenderRequest(httpContext, renderParam ?? RenderParam(httpContext), cancellationToken);

        return AsResult(render, httpContext);

    }

    public Task<RenderActionResult<PostRenderDto>> RenderPageBySlug(string slug, HttpContext httpContext, CancellationToken cancellationToken)
    {
        //TODO: rework it
        //return RenderUrl("/" + slug, httpContext, cancellationToken: cancellationToken);
        return RenderPostBySlug("page", slug, httpContext, cancellationToken: cancellationToken);
    }

    public async Task<RenderActionResult<PostRenderDto>> RenderPostBySlug(string typeName, string slug, HttpContext httpContext, CancellationToken cancellationToken)
    {

        if (disallowRenderTypes.Contains(typeName))
        {
            return await RenderPage404(httpContext, cancellationToken);
        }

        var postService = _serviceProvider.GetRequiredService<IPostService>();
        var post = await postService.GetDetailBySlug(slug, typeName, renderContent: true, cancellationToken);

        var af = httpContext.Items[nameof(MarsAppFront)] as MarsAppFront;
        ArgumentNullException.ThrowIfNull(af, nameof(af));
        if (IsRenderNotSupport(af)) return RenderNotSupportError();

        if (af.Configuration.Mode is AppFrontMode.None or AppFrontMode.HandlebarsTemplate or AppFrontMode.HandlebarsTemplateStatic)
        {
            var page = PostAsWebPage(post);
            if (page is null)
            {
                return await RenderPage404(httpContext, cancellationToken);
            }
            return await RenderPage(page, httpContext, cancellationToken);

        }
        else
        {
            throw new NotImplementedException("resolve page by Id not implement");
        }
    }

    public async Task<RenderActionResult<PostRenderDto>> RenderPostById(Guid postId, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var postService = _serviceProvider.GetRequiredService<IPostService>();
        var post = await postService.GetDetail(postId, renderContent: true, cancellationToken);

        //TODO: надо подумать как рисовать посты по id

        if (disallowRenderTypes.Contains(post.Type))
        {
            return await RenderPage404(httpContext, cancellationToken);
        }

        var af = httpContext.Items[nameof(MarsAppFront)] as MarsAppFront;
        ArgumentNullException.ThrowIfNull(af, nameof(af));
        if (IsRenderNotSupport(af)) return RenderNotSupportError();

        if (af.Configuration.Mode is AppFrontMode.None or AppFrontMode.HandlebarsTemplate or AppFrontMode.HandlebarsTemplateStatic)
        {
            var page = PostAsWebPage(post);
            if (page is null)
            {
                return await RenderPage404(httpContext, cancellationToken);
            }
            return await RenderPage(page, httpContext, cancellationToken);

        }
        else
        {
            throw new NotImplementedException("resolve page by Id not implement");
        }

    }

    private WebPage PostAsWebPage(PostDetail post)
    {
        var filepath = $"/{post.Type}/{post.Id}";
        var attr = new Dictionary<string, string>();
        var page = new WebPage(new WebSitePart(WebSitePartType.Page, post.Slug, filepath, filepath, post.Content!, attr, post.Title), "/" + post.Slug, post.Title);
        return page;
    }

}
