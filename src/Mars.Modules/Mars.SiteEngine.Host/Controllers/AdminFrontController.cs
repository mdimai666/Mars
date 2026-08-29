using System.Net.Mime;
using Mars.Server.Abstractions.ExceptionFilters;
using Mars.Core.Exceptions;
using Mars.SiteEngine.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mars.SiteEngine.Controllers;

/// <summary>
/// Отдельные эндпоинты для отрисовки специального фронта админки (data/admin/front).
/// Эти шаблоны отображаются только в админ-панели и не роутятся публично.
/// </summary>
[ApiController]
[Route("api/[controller]/[action]")]
[Authorize(Roles = "Admin")]
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
}
