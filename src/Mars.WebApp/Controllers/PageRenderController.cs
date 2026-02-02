using System.Net.Mime;
using System.Web;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Host.Shared.Mappings.Renders;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Services;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Renders;
using Mars.UseStartup;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[NotFoundExceptionFilter]
[FluentValidationExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
public class PageRenderController : ControllerBase
{
    private readonly IPageRenderService _pageRenderService;

    public PageRenderController(IPageRenderService pageRenderService)
    {
        _pageRenderService = pageRenderService;
    }

    [HttpGet("by-id/{id:guid}")]
    public async Task<RenderActionResult<PostRenderResponse>> Render(Guid id, CancellationToken cancellationToken)
    {
        SetupAppFront();
        return (await _pageRenderService.RenderPostById(id, HttpContext, cancellationToken)).ToResponse();
    }

    //[HttpGet("Render/{type}/{id:guid}")]
    //[Obsolete]
    //public async Task<ActionResult<RenderActionResult<PostRenderDto>>> Render(string type, Guid id, CancellationToken cancellationToken)
    //{
    //    return _pageRenderService.RenderPostById(id, HttpContext, cancellationToken);
    //}

    [HttpGet("by-post/{type}/{slug}")]
    public async Task<RenderActionResult<PostRenderResponse>> RenderPost(string type, string slug, CancellationToken cancellationToken)
    {
        SetupAppFront();
        return (await _pageRenderService.RenderPostBySlug(type, slug, HttpContext, cancellationToken)).ToResponse();
    }

    [HttpGet("by-slug/{slug}")]
    public async Task<RenderActionResult<PostRenderResponse>> Render(string slug, CancellationToken cancellationToken)
    {
        SetupAppFront();
        return (await _pageRenderService.RenderPageBySlug(slug, HttpContext, cancellationToken)).ToResponse();
    }

    [HttpGet("by-url")]
    public async Task<RenderActionResult<PostRenderResponse>> RenderUrl([FromQuery] string url, CancellationToken cancellationToken)
    {
        SetupAppFront();
        return (await _pageRenderService.RenderUrl(HttpUtility.UrlDecode(url), HttpContext)).ToResponse();
    }

    private void SetupAppFront()
    {
        //MarsAppFront app = StartupFront.AppProvider.GetAppForUrl(url);
        MarsAppFront app = StartupFront.AppProvider.FirstApp;
        HttpContext.Items.TryAdd(nameof(MarsAppFront), app);
    }
}
