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

namespace Mars.Services;

internal class PageRenderService : IPageRenderService
{
    public PageRenderService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    string[] disallowRenderTypes = { "template", "block", "layout" };
    private readonly IServiceProvider _serviceProvider;

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

    public Task<RenderActionResult<PostRenderDto>> RenderPageOr404(WebPage? page, HttpContext httpContext, RenderParam? renderParam, CancellationToken cancellationToken)
        => page == null ? RenderPage404(httpContext, cancellationToken)
                        : RenderPage(page, httpContext, renderParam, cancellationToken);

    public async Task<RenderActionResult<PostRenderDto>> RenderPage(WebPage page, HttpContext httpContext, RenderParam? renderParam, CancellationToken cancellationToken)
    {
        var af = httpContext.Items[nameof(MarsAppFront)] as MarsAppFront;
        var request = new WebClientRequest(httpContext.Request, replacePath: page.Url);

        var tsv = af.Features.Get<IWebTemplateService>();
        WebSiteProcessor.Services.WebSiteRequestProcessor processor = new(_serviceProvider, tsv.Template);
        var render = await processor.RenderPage(af, request, page, RenderParam(httpContext), cancellationToken);

        return AsResult(render, httpContext);
    }

    public async Task<RenderActionResult<PostRenderDto>> RenderPage404(HttpContext httpContext, CancellationToken cancellationToken)
    {
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
            Message = "AppFront mode not support render",
            Data = null!
        };

    bool IsRenderNotSupport(MarsAppFront af)
    {
        var renderEngine = af.Features.Get<IWebRenderEngine>();
        return renderEngine is null;
    }

    public async Task<RenderActionResult<PostRenderDto>> RenderUrl(PathString url, HttpContext httpContext, RenderParam? renderParam = null, CancellationToken cancellationToken = default)
    {
        var af = httpContext.Items[nameof(MarsAppFront)] as MarsAppFront;
        ArgumentNullException.ThrowIfNull(af, nameof(af));

        if (IsRenderNotSupport(af)) return RenderNotSupportError();

        var tsv = af.Features.Get<IWebTemplateService>();
        WebPage? page = tsv.Template.CompiledHttpRouteMatcher.Match(httpContext.Request.Path, out var routeValues);

        var render = await RenderPageOr404(page, httpContext, renderParam, cancellationToken);

        return render;
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
            if (post is null)
                return await RenderPage404(httpContext, cancellationToken);

            var page = PostAsWebPage(post);
            return await RenderPage(page, httpContext, null, cancellationToken);
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
            return await RenderPage(page, httpContext, null, cancellationToken);

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
