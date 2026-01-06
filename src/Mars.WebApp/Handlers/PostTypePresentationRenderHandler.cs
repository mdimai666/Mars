using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite.Exceptions;
using Mars.Host.Shared.WebSite.Interfaces;
using Mars.Host.Shared.WebSite.Models;
using Mars.Shared.Contracts.WebSite.Models;
using Mars.Shared.Models;
using Mars.WebSiteProcessor.Services;

namespace Mars.Handlers;

public class PostTypePresentationRenderHandler(IPostService postService)
{
    public async Task<string?> Handle(SourceUri sourceUri, string? queryString, HttpContext httpContext, CancellationToken cancellationToken)
    {
        if (sourceUri.SegmentsCount != 2)
            throw MarsValidationException.FromSingleError("sourceUri", "sourceUri.SegmentsCount should be 2");

        var post = await postService.GetDetailBySlug(slug: sourceUri[1], type: sourceUri[0], renderContent: false, cancellationToken);
        if (post is null) return null;

        try
        {
            var af = httpContext.Items[nameof(MarsAppFront)] as MarsAppFront;
            var request = new WebClientRequest(httpContext.Request, replaceQueryString: queryString ?? "");

            var tsv = af.Features.Get<IWebTemplateService>();
            WebSiteRequestProcessor processor = new(httpContext.RequestServices, tsv.Template);
            var page = PostAsWebPage(post);
            var render = await processor.RenderPage(af, request, page, new RenderParam() { OnlyBody = true, AllowLayout = true }, cancellationToken);

            return render.html;
        }
        catch (RenderPageHtmlException ex)
        {
            var msg = $"""
                <div class="alert alert-warning">
                    <pre>
                    Message: {ex.Message}
                    Page: {ex.Page.Name}
                    PagePath: {ex.Page.FileFullPath}\n\n

                    StackTrace: {ex.StackTrace}\n
                    </pre>
                </div>
                """;
            return msg;
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
