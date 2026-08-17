using System.Net.Mime;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite;
using Mars.Host.Shared.WebSite.Interfaces;
using Mars.Options.Models;
using Microsoft.AspNetCore.Http;

namespace Mars.Options.Host;

/// <summary>
/// Обработчик пайплайна фронтов для режима обслуживания: закрывает фронты страницей обслуживания.
/// Админка и прочие endpoint'ы не затрагиваются — они исполняются endpoint'ами и не принадлежат
/// пайплайну фронтов. API рендера фронтов (PageRender) закрывается только при включённой
/// <see cref="MaintenanceModeOption.EnableForApiRender"/> — иначе мобильные приложения
/// и другие потребители API продолжают работать, когда «сайт» закрыт.
/// </summary>
public class MaintenanceFrontRequestHandler(IOptionService optionService, IWebSiteProcessor webSiteProcessor) : IFrontRequestHandler
{
    public int Order => 100;

    public async Task<bool> HandleAsync(HttpContext httpContext, MarsAppFront appFront, CancellationToken cancellationToken)
    {
        var option = optionService.GetOption<MaintenanceModeOption>();
        if (!option.Enable)
            return false;

        var isApiRenderEndpoint = httpContext.GetEndpoint()?.Metadata.GetMetadata<FrontRenderEndpointAttribute>() is not null;
        if (isApiRenderEndpoint)
        {
            if (!option.EnableForApiRender)
                return false;

            // форма ответа совместима с RenderActionResult (ok/message/notFound/data)
            httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await httpContext.Response.WriteAsJsonAsync(new
            {
                Ok = false,
                Message = "Режим обслуживания",
                NotFound = false,
                Data = (object?)null,
            }, cancellationToken);
            return true;
        }

        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        httpContext.Response.ContentType = MediaTypeNames.Text.Html;

        if (option.MaintenancePageSource == EMaintenancePageSource.FrontPage)
        {
            var template = appFront.Features.Get<IWebTemplateService>().Template;
            var page = template.Pages.FirstOrDefault(s => s.Url == option.RenderPageUrl);

            if (page is null)
            {
                await httpContext.Response.WriteAsync($"Maintenance page '{option.RenderPageUrl}' not found", cancellationToken);
                return true;
            }

            httpContext.Items.TryAdd(nameof(MarsAppFront), appFront);
            var render = await webSiteProcessor.RenderPage(page, httpContext, null, cancellationToken);
            await httpContext.Response.WriteAsync(render.html, cancellationToken);
            return true;
        }

        await httpContext.Response.WriteAsync(option.MaintenanceStaticPageContent, cancellationToken);
        return true;
    }
}
