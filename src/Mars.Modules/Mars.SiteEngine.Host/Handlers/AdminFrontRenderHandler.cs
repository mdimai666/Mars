using Mars.Core.Exceptions;
using Mars.Server.Abstractions.Models;
using Mars.SiteEngine.Abstractions.Constants.Website;
using Mars.SiteEngine.Abstractions.Models;
using Mars.SiteEngine.Abstractions.WebSite;
using Mars.SiteEngine.Abstractions.WebSite.Interfaces;
using Mars.SiteEngine.Abstractions.WebSite.Models;
using Mars.SiteEngine.Host.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Mars.SiteEngine.Host.Handlers;

/// <summary>
/// Рендер страниц специального фронта админки (data/admin/front).
/// Не кэширует: форсирует перескан шаблона, чтобы правки в редакторе были видны сразу.
/// Рендерит только тело (без _root) — админка имеет собственную обвязку.
/// </summary>
public class AdminFrontRenderHandler(IWebRenderEngineLocator renderEngineLocator)
{
    public async Task<string?> RenderByFile(string fileRelPath, string? queryString, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var (app, template) = GetAdminTemplate();

        var page = template.Pages.FirstOrDefault(s => Norm(s.FileRelPath) == Norm(fileRelPath));
        if (page is null) return null;

        return await RenderPage(app, template, page, page.Url, queryString, routeValues: null, httpContext, cancellationToken);
    }

    /// <summary>
    /// Рендер по URL из директивы @page — адрес /dev/p/{path} в админке.
    /// Пустой путь — индексная страница (@page "/").
    /// </summary>
    public async Task<string?> RenderByPath(string? path, string? queryString, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var (app, template) = GetAdminTemplate();

        var url = new PathString("/" + (path ?? "").Trim('/'));
        var page = template.WebPageRouteMatcher.Match(url, out var routeValues);
        if (page is null) return null;

        return await RenderPage(app, template, page, url, queryString, routeValues, httpContext, cancellationToken);
    }

    (MarsAppFront App, WebSiteTemplate Template) GetAdminTemplate()
    {
        var app = renderEngineLocator.GetAppFrontBySlug(AppAdminConstants.AdminFrontSlug)
            ?? throw new NotFoundException($"Admin front '{AppAdminConstants.AdminFrontSlug}' not found");

        var tsv = app.Features.Get<IWebTemplateService>();

        // Форсируем перескан: watcher обновляет шаблон с debounce,
        // а после сохранения в редакторе результат нужен сразу.
        tsv.ScanSite();

        return (app, tsv.Template);
    }

    async Task<string> RenderPage(MarsAppFront app, WebSiteTemplate template, WebPage page, PathString path,
        string? queryString, RouteValueDictionary? routeValues, HttpContext httpContext, CancellationToken cancellationToken)
    {
        // queryString пробрасывается отдельно (RemotePageViewer передаёт его параметром),
        // чтобы в шаблоне был доступен _req.Query (например page для пагинации).
        var request = string.IsNullOrEmpty(queryString)
            ? new WebClientRequest(httpContext.Request, replacePath: path, routeValues: routeValues)
            : new WebClientRequest(httpContext.Request, replacePath: path, routeValues: routeValues, replaceQueryString: queryString);

        // WebClientRequest копирует routeValues в свой словарь — пуловский возвращаем сразу
        if (routeValues is not null)
            WebPageRouteMatcher.RouteValuePools.Return(routeValues);

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