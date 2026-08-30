using System.Net.Mime;
using Mars.Server.Abstractions.Models;
using Mars.SiteEngine.Abstractions.Models;
using Mars.SiteEngine.Abstractions.WebSite;
using Mars.SiteEngine.Abstractions.WebSite.Exceptions;
using Mars.SiteEngine.Abstractions.WebSite.Interfaces;
using Mars.SiteEngine.Abstractions.WebSite.Models;
using Mars.SiteEngine.Host.Services;
using Microsoft.AspNetCore.Http;

namespace Mars.SiteEngine.Host.Endpoints;

public class MapWebSiteProcessor : IWebSiteProcessor
{
    public async Task Response(HttpContext httpContext, CancellationToken cancellationToken)
    {

#if DEBUG
        //Console.WriteLine("MapWebSiteProcessor::Start> " + httpContext.Request.Path);
#endif
        var _serviceProvider = httpContext.RequestServices;

        httpContext.Response.ContentType = MediaTypeNames.Text.Html;

        var af = httpContext.Items[nameof(MarsAppFront)] as MarsAppFront;
        var request = new WebClientRequest(httpContext.Request);

        var tsv = af.Features.Get<IWebTemplateService>();

        WebSiteRequestProcessor processor;
        try
        {
            // Template сканирует папку фронта: ошибки пути/структуры шаблона ловим отдельно —
            // Page500 из сломанного шаблона отрисовать всё равно не получится
            processor = new WebSiteRequestProcessor(_serviceProvider, tsv.Template);
        }
        catch (Exception ex)
        {
            httpContext.Response.StatusCode = 500;
            await httpContext.Response.WriteAsync($"""
                <pre>
                Front template error
                Front: {af.Front?.Slug}
                Path: {af.Configuration.Path}
                Message: {ex.Message}
                </pre>
                """);
            return;
        }

        try
        {

            var render = await processor.RenderRequest(httpContext, new RenderParam(), cancellationToken);

            httpContext.Response.StatusCode = render.Code;
            await httpContext.Response.WriteAsync(render.html);

        }
        catch (RenderPageHtmlException ex)
        {

            string msg;

            if (tsv.Template.Page500 is not null)
            {
                //httpContext.Items.Add(nameof(RenderPageHtmlException), ex);
                request.Items.Add(typeof(RenderPageHtmlException), ex);
                var render = await processor.RenderPage(af, request, tsv.Template.Page500, new RenderParam(), cancellationToken);
                msg = render.html;
            }
            else
            {
                msg = $"""
                    <pre>
                    Message: {ex.Message}
                    Page: {ex.Page.Name}
                    PagePath: {ex.Page.FileFullPath}\n\n

                    StackTrace: {ex.StackTrace}\n
                    </pre>
                """;
            }

            httpContext.Response.StatusCode = 500;
            await httpContext.Response.WriteAsync(msg);
        }
        catch (Exception ex)
        {
            httpContext.Response.StatusCode = 500;
#if DEBUG
            await httpContext.Response.WriteAsync($"<pre>{ex.Message}\n\n{ex.StackTrace}</pre>");
#else
            await httpContext.Response.WriteAsync($"<pre>{ex.Message}</pre>");
#endif
        }
        finally
        {
            //scope.Dispose();
#if DEBUG
            //Console.WriteLine("MapWebSiteProcessor::End > " + httpContext.Request.Path);
#endif
        }
    }

    public async Task<RenderInfo> RenderRequest(HttpContext httpContext, RenderParam? param, CancellationToken cancellationToken)
    {
        var af = httpContext.Items[nameof(MarsAppFront)] as MarsAppFront;
        var tsv = af.Features.Get<IWebTemplateService>();
        var serviceProvider = httpContext.RequestServices;
        WebSiteRequestProcessor processor = new(serviceProvider, tsv.Template);
        return await processor.RenderRequest(httpContext, param ?? new(), cancellationToken);

    }

    public async Task<RenderInfo> RenderPage(WebPage page, HttpContext httpContext, RenderParam? param, CancellationToken cancellationToken)
    {
        var af = httpContext.Items[nameof(MarsAppFront)] as MarsAppFront;
        var request = new WebClientRequest(httpContext.Request);

        var tsv = af.Features.Get<IWebTemplateService>();
        var serviceProvider = httpContext.RequestServices;
        WebSiteRequestProcessor processor = new(serviceProvider, tsv.Template);
        return await processor.RenderPage(af, request, page, param ?? new(), cancellationToken);
    }

    public async Task<RenderInfo> RenderPage404(HttpContext httpContext, RenderParam? param, CancellationToken cancellationToken)
    {
        var af = httpContext.Items[nameof(MarsAppFront)] as MarsAppFront;
        var request = new WebClientRequest(httpContext.Request);

        var tsv = af.Features.Get<IWebTemplateService>();
        var serviceProvider = httpContext.RequestServices;
        WebSiteRequestProcessor processor = new(serviceProvider, tsv.Template);
        return await processor.RenderPage404(af, request, param ?? new(), cancellationToken);
    }
}
