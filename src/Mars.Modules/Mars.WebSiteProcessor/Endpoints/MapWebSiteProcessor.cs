using System.Net.Mime;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite;
using Mars.Host.Shared.WebSite.Exceptions;
using Mars.Host.Shared.WebSite.Interfaces;
using Mars.Host.Shared.WebSite.Models;
using Mars.WebSiteProcessor.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebSiteProcessor.Endpoints;

public class MapWebSiteProcessor : IWebSiteProcessor
{
    public async Task Response(HttpContext httpContext, CancellationToken cancellationToken)
    {

#if DEBUG
        //Console.WriteLine("MapWebSiteProcessor::Start> " + httpContext.Request.Path);
#endif
        var _serviceProvider = httpContext.RequestServices;

        httpContext.Response.ContentType = MediaTypeNames.Text.Html;

        IMarsAppProvider appProvider = _serviceProvider.GetRequiredService<IMarsAppProvider>();

        var af = httpContext.Items[nameof(MarsAppFront)] as MarsAppFront;
        var request = new WebClientRequest(httpContext.Request);

        var tsv = af.Features.Get<IWebTemplateService>();
        WebSiteRequestProcessor processor = new WebSiteRequestProcessor(_serviceProvider, tsv.Template);

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
#if DEBUG
            httpContext.Response.StatusCode = 500;
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
        WebSiteRequestProcessor processor = new WebSiteRequestProcessor(serviceProvider, tsv.Template);
        return await processor.RenderRequest(httpContext, param ?? new(), cancellationToken);

    }

    public async Task<RenderInfo> RenderPage(WebPage page, HttpContext httpContext, RenderParam? param, CancellationToken cancellationToken)
    {
        var af = httpContext.Items[nameof(MarsAppFront)] as MarsAppFront;
        var request = new WebClientRequest(httpContext.Request);

        var tsv = af.Features.Get<IWebTemplateService>();
        var serviceProvider = httpContext.RequestServices;
        WebSiteRequestProcessor processor = new WebSiteRequestProcessor(serviceProvider, tsv.Template);
        return await processor.RenderPage(af, request, page, param ?? new(), cancellationToken);
    }

    public async Task<RenderInfo> RenderPage404(HttpContext httpContext, RenderParam? param, CancellationToken cancellationToken)
    {
        var af = httpContext.Items[nameof(MarsAppFront)] as MarsAppFront;
        var request = new WebClientRequest(httpContext.Request);

        var tsv = af.Features.Get<IWebTemplateService>();
        var serviceProvider = httpContext.RequestServices;
        WebSiteRequestProcessor processor = new WebSiteRequestProcessor(serviceProvider, tsv.Template);
        return await processor.RenderPage404(af, request, param ?? new(), cancellationToken);
    }

    public WebPage? ResolveUrl(PathString path, HttpContext httpContext)
    {
        var af = httpContext.Items[nameof(MarsAppFront)] as MarsAppFront;
        var tsv = af.Features.Get<IWebTemplateService>();
        //var serviceProvider = httpContext.RequestServices;
        //WebSiteRequestProcessor processor = new WebSiteRequestProcessor(serviceProvider, tsv.Template);
        //return processor.ResolveUrl(path);
        return tsv.Template.CompiledHttpRouteMatcher.Match(path, out _);
    }
}
