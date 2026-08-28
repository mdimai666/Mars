using System.Net.Mime;
using Mars.SiteEngine.Abstractions.WebSite;
using Mars.Server.Abstractions.ExceptionFilters;
using System.Web;
using Mars.Core.Exceptions;
using Mars.SiteEngine.Abstractions.Services;
using Mars.Server.Abstractions.Models;
using Mars.SiteEngine.Abstractions.Models;
using Mars.SiteEngine.Abstractions.Services;
using Mars.Services;
using Mars.Contracts.Common;
using Mars.SiteEngine.Contracts.Renders;
using Mars.SiteEngine.Interfaces;
using Mars.SiteEngine.Services;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[NotFoundExceptionFilter]
[FluentValidationExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
[FrontRenderEndpoint]
public class PageRenderController : ControllerBase
{
    private readonly IPageRenderService _pageRenderService;
    private readonly IWebRenderEngineLocator _renderEngineLocator;

    public PageRenderController(IPageRenderService pageRenderService, IWebRenderEngineLocator renderEngineLocator)
    {
        _pageRenderService = pageRenderService;
        _renderEngineLocator = renderEngineLocator;
    }

    [HttpGet("by-id/{id:guid}")]
    public async Task<RenderActionResult<PostRenderResponse>> RenderById(Guid id, [FromQuery] string? frontSlug, CancellationToken cancellationToken)
    {
        SetupAppFront(frontSlug);
        return (await _pageRenderService.RenderPostById(id, HttpContext, cancellationToken)).ToResponse();
    }

    //[HttpGet("Render/{type}/{id:guid}")]
    //[Obsolete]
    //public async Task<ActionResult<RenderActionResult<PostRenderDto>>> Render(string type, Guid id, CancellationToken cancellationToken)
    //{
    //    return _pageRenderService.RenderPostById(id, HttpContext, cancellationToken);
    //}

    [HttpGet("by-post/{type}/{slug}")]
    public async Task<RenderActionResult<PostRenderResponse>> RenderPost(string type, string slug, [FromQuery] string? frontSlug, CancellationToken cancellationToken)
    {
        SetupAppFront(frontSlug);
        return (await _pageRenderService.RenderPostBySlug(type, slug, HttpContext, cancellationToken)).ToResponse();
    }

    [HttpGet("by-slug/{slug}")]
    public async Task<RenderActionResult<PostRenderResponse>> RenderPageBySlug(string slug, [FromQuery] string? frontSlug, CancellationToken cancellationToken)
    {
        SetupAppFront(frontSlug);
        return (await _pageRenderService.RenderPageBySlug(slug, HttpContext, cancellationToken)).ToResponse();
    }

    [HttpGet("by-url")]
    public async Task<RenderActionResult<PostRenderResponse>> RenderUrl([FromQuery] string url, [FromQuery] string? frontSlug, CancellationToken cancellationToken)
    {
        SetupAppFront(frontSlug);
        return (await _pageRenderService.RenderUrl(HttpUtility.UrlDecode(url), HttpContext)).ToResponse();
    }

    private void SetupAppFront(string? frontSlug)
    {
        MarsAppFront app;
        if (string.IsNullOrWhiteSpace(frontSlug))
        {
            app = _renderEngineLocator.GetAppFrontForUrl("/")
                ?? throw new NotFoundException("Default front not found");
        }
        else
        {
            // Админ-фронт рендерится только через /api/AdminFront/Render —
            // наружу отдаём 404, чтобы не раскрывать его существование.
            if (string.Equals(frontSlug, FrontManager.AdminFrontSlug, StringComparison.OrdinalIgnoreCase))
                throw new NotFoundException($"Front '{frontSlug}' not found");

            app = _renderEngineLocator.GetAppFrontBySlug(frontSlug)
                ?? throw new NotFoundException($"Front '{frontSlug}' not found");
        }
        HttpContext.Items.TryAdd(nameof(MarsAppFront), app);
    }
}
