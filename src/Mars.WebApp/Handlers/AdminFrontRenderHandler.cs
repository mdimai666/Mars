using Mars.Core.Exceptions;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.WebSite.Interfaces;
using Mars.Host.Shared.WebSite.Models;
using Mars.Services;
using Mars.WebSiteProcessor.Interfaces;
using Mars.WebSiteProcessor.Services;

namespace Mars.Handlers;

/// <summary>
/// Рендер страниц специального фронта админки (data/admin/front).
/// Не кэширует: форсирует перескан шаблона, чтобы правки в редакторе были видны сразу.
/// Рендерит только тело (без _root) — админка имеет собственную обвязку.
/// </summary>
public class AdminFrontRenderHandler(IWebRenderEngineLocator renderEngineLocator)
{
    public async Task<string?> RenderByFile(string fileRelPath, string? queryString, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var app = renderEngineLocator.GetAppFrontBySlug(FrontManager.AdminFrontSlug)
            ?? throw new NotFoundException($"Admin front '{FrontManager.AdminFrontSlug}' not found");

        var tsv = app.Features.Get<IWebTemplateService>();

        // Форсируем перескан: файлы админ-фронта редактируются в редакторе,
        // а FileSystemWatcher в WebTemplateService выключен.
        tsv.ScanSite();

        var template = tsv.Template;
        var page = template.Pages.FirstOrDefault(s => Norm(s.FileRelPath) == Norm(fileRelPath));
        if (page is null) return null;

        // queryString пробрасывается отдельно (RemotePageViewer передаёт его параметром),
        // чтобы в шаблоне был доступен _req.Query (например page для пагинации).
        var request = string.IsNullOrEmpty(queryString)
            ? new WebClientRequest(httpContext.Request, replacePath: page.Url)
            : new WebClientRequest(httpContext.Request, replacePath: page.Url, replaceQueryString: queryString);

        var processor = new WebSiteRequestProcessor(httpContext.RequestServices, template);

        // Рендер через _root (в нём только @Body) — на выходе лишь контент страницы,
        // обвязку админка добавляет сама.
        var render = await processor.RenderPage(app, request, page,
            new RenderParam { OnlyBody = false, AllowLayout = false, UseCache = false },
            cancellationToken);

        return render.html;
    }

    static string Norm(string path) => path.Replace('\\', '/').TrimStart('/');
}
