using System.Net.Mime;
using Mars.Core.Exceptions;
using Mars.Identity.Contracts.ViewModels;
using Mars.Server.Abstractions.Authorization;
using Mars.Server.Abstractions.ExceptionFilters;
using Mars.SiteEngine.Host.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace Mars.SiteEngine.Host.Controllers;

/// <summary>
/// Отдельные эндпоинты для отрисовки специального фронта админки (data/admin/front).
/// Эти шаблоны отображаются только в админ-панели и не роутятся публично.
/// </summary>
[ApiController]
[Route("api/[controller]/[action]")]
[Cap(RoleCaps.FrontCap.Manage)]
[UserActionResultExceptionFilter]
[NotFoundExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
public class AdminFrontController : ControllerBase
{
    private readonly AdminFrontRenderHandler _adminFrontRenderHandler;

    public AdminFrontController(AdminFrontRenderHandler adminFrontRenderHandler)
    {
        _adminFrontRenderHandler = adminFrontRenderHandler;
    }

    /// <summary>
    /// Рендер страницы админ-фронта по относительному пути файла (например admin_index.hbs).
    /// queryString пробрасывается в шаблон как _req.Query (используется RemotePageViewer).
    /// </summary>
    [HttpGet]
    [Produces(MediaTypeNames.Text.Html)]
    public async Task<IActionResult> Render(string file, string? queryString, CancellationToken cancellationToken)
    {
        var html = await _adminFrontRenderHandler.RenderByFile(file, queryString, HttpContext, cancellationToken)
            ?? throw new NotFoundException($"Страница админ-фронта '{file}' не найдена");

        return Content(html, MediaTypeNames.Text.Html);
    }

    /// <summary>
    /// Рендер страницы админ-фронта по URL из директивы @page (например / или /help) —
    /// адрес /dev/p/{path} в админке. Пустой path — индексная страница.
    /// queryString пробрасывается в шаблон как _req.Query (используется RemotePageViewer).
    /// </summary>
    [HttpGet]
    [Produces(MediaTypeNames.Text.Html)]
    public async Task<IActionResult> RenderByPath(string? path, string? queryString, CancellationToken cancellationToken)
    {
        var html = await _adminFrontRenderHandler.RenderByPath(path, queryString, HttpContext, cancellationToken)
            ?? throw new NotFoundException($"Страница админ-фронта '{path}' не найдена");

        return Content(html, MediaTypeNames.Text.Html);
    }
}
